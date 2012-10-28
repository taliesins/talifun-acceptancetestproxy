using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Talifun.AcceptanceTestProxy.UrlRewriter.Conditions;
using Talifun.AcceptanceTestProxy.UrlRewriter.Conditions.Flags;
using Talifun.AcceptanceTestProxy.UrlRewriter.Rules;
using Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags;
using log4net;
using NoCaseFlag = Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags.NoCaseFlag;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Engines
{
    public class MicrosoftRuleSet : RuleSet
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MicrosoftRuleSet));

        private FileInfo _ruleSetConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftRuleSet"/> class.
        /// </summary>
        /// <param name="ruleSetConfig">The rule set config.</param>
        public MicrosoftRuleSet(FileInfo ruleSetConfig)
        {
            PhysicalBase = "/";
            _ruleSetConfig = ruleSetConfig;
        }

        /// <summary>
        /// Refreshes the rules.
        /// </summary>
        public void RefreshRules()
        {
            using (StreamReader reader = _ruleSetConfig.OpenText())
            {
                RefreshRules(reader, "/configuration/system.webServer/rewrite/rules/rule");
            }
        }

        /// <summary>
        /// Refreshes the rules.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="xpath"> </param>
        public void RefreshRules(StreamReader reader, string xpath)
        {
            var configuration = new XmlDocument();

            // load the configuration
            configuration.Load(reader);

            // get the rules
            var rules = configuration.SelectNodes(xpath);

            AddRules(rules);
        }

        /// <summary>
        /// Adds the rules.
        /// </summary>
        /// <param name="ruleElements">The rule elements.</param>
        public void AddRules(XmlNodeList ruleElements)
        {
            foreach (XmlNode ruleElement in ruleElements)
            {
                var rule = GetRule(ruleElement);

                if (rule != null)
                {
                    AddRule(rule);
                }
            }
        }

        /// <summary>
        /// Gets the rule.
        /// </summary>
        /// <param name="ruleElement">The rule element.</param>
        /// <returns></returns>
        private IRule GetRule(XmlNode ruleElement)
        {
            if (ruleElement == null)
            {
                throw new ArgumentNullException("ruleElement");
            }

            if (ruleElement.Name != "rule")
            {
                throw new RuleSetException("The node is not a \"rule\".");
            }

            bool enabled = true; // from schema definition

            if (ruleElement.Attributes["enabled"] != null)
            {
                enabled = XmlConvert.ToBoolean(ruleElement.Attributes["enabled"].Value);
            }

            // if it is not enabled there is no reason to continue processing
            if (!enabled)
            {
                return null;
            }

            var name = string.Empty;
            var stopProcessing = false; // from schema definition
            var patternSyntax = "ECMAScript"; // from schema definiton

            if (ruleElement.Attributes["name"] != null)
            {
                name = ruleElement.Attributes["name"].Value;
            }

            if (ruleElement.Attributes["stopProcessing"] != null)
            {
                stopProcessing = XmlConvert.ToBoolean(ruleElement.Attributes["stopProcessing"].Value);
            }

            if (ruleElement.Attributes["patternSyntax"] != null)
            {
                patternSyntax = ruleElement.Attributes["patternSyntax"].Value;
            }

            var matchElement = ruleElement.SelectSingleNode("match");
            var conditionsElement = ruleElement.SelectSingleNode("conditions");
            var serverVariablesElement = ruleElement.SelectSingleNode("serverVariables");
            var actionElement = ruleElement.SelectSingleNode("action");

            IRuleFlagProcessor ruleFlags = new RuleFlagProcessor();
            IRule rule = new DefaultRule();
            rule.Name = name;

            // <match />
            var match = GetMatch(matchElement, ref ruleFlags);

            // <condition />
            var conditions = GetConditions(conditionsElement);

            // <serverVariables />
            foreach (var flag in GetServerVariables(serverVariablesElement))
            {
                ruleFlags.Add(flag);
            }
            

            // <action />
            var action = GetAction(actionElement, match, ref ruleFlags);

            // <rule />
            rule.Init(conditions, action, ruleFlags);

            return rule;
        }

        private enum ActionType
        {
            None = 0,
            Rewrite,
            Redirect,
            CustomResponse,
            AbortRequest
        }

        private enum RedirectType
        {
            Permanent = 301,
            Found = 302,
            SeeOther = 303,
            Temporary = 307
        }

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <param name="actionElement">The action element.</param>
        /// <param name="ruleFlags">The rule flags.</param>
        /// <returns></returns>
        private IRuleAction GetAction(XmlNode actionElement, Pattern pattern, ref IRuleFlagProcessor ruleFlags)
        {
            var type = ActionType.None;
            string url = null;
            var appendQueryString = true; // from schema definition
            var redirectType = RedirectType.Permanent;
            var statusCode = 0U;
            var subStatusCode = 0U; // from schema definition
            string statusReason = null;
            string statusDescription = null;

            if (actionElement.Attributes["type"] != null)
            {
                try { type = (ActionType)Enum.Parse(typeof(ActionType), actionElement.Attributes["type"].Value, true); }
                catch (Exception exc)
                {
                    Logger.Error("Action: " + exc.Message, exc);
                }
            }

            if (actionElement.Attributes["url"] != null)
            {
                url = actionElement.Attributes["url"].Value;
            }

            if (actionElement.Attributes["appendQueryString"] != null)
            {
                appendQueryString = XmlConvert.ToBoolean(actionElement.Attributes["appendQueryString"].Value);
            }

            if (actionElement.Attributes["redirectType"] != null)
            {
                try { redirectType = (RedirectType)Enum.Parse(typeof(RedirectType), actionElement.Attributes["redirectType"].Value, true); }
                catch (Exception exc) { Logger.Error("Action: " + exc.Message, exc); }
            }

            if (actionElement.Attributes["statusCode"] != null)
            {
                statusCode = XmlConvert.ToUInt32(actionElement.Attributes["statusCode"].Value);
            }

            if (actionElement.Attributes["subStatusCode"] != null)
            {
                subStatusCode = XmlConvert.ToUInt32(actionElement.Attributes["subStatusCode"].Value);
            }

            if (actionElement.Attributes["statusReason"] != null)
                statusReason = actionElement.Attributes["statusReason"].Value;

            if (actionElement.Attributes["statusDescription"] != null)
            {
                statusDescription = actionElement.Attributes["statusDescription"].Value;
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new RuleSetException("Action URL must be a non-empty value.");
            }

            // validationType="requireTrimmedString"
            url = url.Trim();

            if (type == ActionType.Redirect)
            {
                ruleFlags.Add(new RedirectFlag((int)redirectType));
            }
            else if (statusCode > 0U)
            {
                // validationType="integerRange" validationParameter="300,307,exclude"
                if (statusCode >= 300U && statusCode <= 307U)
                {
                    throw new RuleSetException("Action Status Code should not be an int between 300 - 307, use the redirectType for this range.");
                }

                if (statusCode < 1U || statusCode > 999U)
                {
                    throw new RuleSetException("Action Status Code should be between 1 - 999.");
                }

                if (subStatusCode < 0U || subStatusCode > 999U)
                {
                    throw new RuleSetException("Action Sub Status Code should be between 0 - 999.");
                }

                ruleFlags.Add(new ResponseStatusFlag(statusCode, subStatusCode, statusReason, statusDescription));
            }

            IRuleAction substitution = new DefaultRuleAction();
            substitution.Init(pattern, url);

            return substitution;
        }

        /// <summary>
        /// Gets the server variables.
        /// </summary>
        /// <param name="serverVariablesElement">The server variables element.</param>
        /// <param name="ruleFlags">The rule flags.</param>
        /// <returns></returns>
        private IEnumerable<ServerVariableFlag> GetServerVariables(XmlNode serverVariablesElement)
        {
            if (serverVariablesElement == null)
            {
                yield break;
            }
            // process each server variable
            foreach (XmlNode serverVariableElement in serverVariablesElement.SelectNodes("set"))
            {
                yield return GetServerVariable(serverVariableElement);
            }
        }

        /// <summary>
        /// Gets the server variable.
        /// </summary>
        /// <param name="serverVariableElement">The server variable element.</param>
        /// <returns></returns>
        private ServerVariableFlag GetServerVariable(XmlNode serverVariableElement)
        {
            string name = null;
            var value = String.Empty;
            var replace = true; // from schema definition

            if (serverVariableElement.Attributes["name"] != null)
            {
                name = serverVariableElement.Attributes["name"].Value;
            }

            if (serverVariableElement.Attributes["value"] != null)
            {
                value = serverVariableElement.Attributes["value"].Value;
            }

            if (serverVariableElement.Attributes["replace"] != null)
            {
                replace = XmlConvert.ToBoolean(serverVariableElement.Attributes["replace"].Value);
            }

            // required="true"
            if (string.IsNullOrEmpty(name))
            {
                throw new RuleSetException("Server Variable Name must be a non-empty value.");
            }

            // validationType="requireTrimmedString"
            name = name.Trim();

            return new ServerVariableFlag(name, value, replace);
        }

        /// <summary>
        /// Gets the match.
        /// </summary>
        /// <param name="matchElement">The match element.</param>
        /// <returns></returns>
        private Pattern GetMatch(XmlNode matchElement, ref IRuleFlagProcessor ruleFlags)
        {
            string url = null;
            var ignoreCase = true; // from schema definition
            var negate = false; // from schema definition

            if (matchElement.Attributes["url"] != null)
            {
                url = matchElement.Attributes["url"].Value;
            }

            if (matchElement.Attributes["ignoreCase"] != null)
            {
                ignoreCase = XmlConvert.ToBoolean(matchElement.Attributes["ignoreCase"].Value);
            }

            if (matchElement.Attributes["negate"] != null)
            {
                negate = XmlConvert.ToBoolean(matchElement.Attributes["negate"].Value);
            }

            // validationType="nonEmptyString"
            if (string.IsNullOrEmpty(url))
            {
                throw new RuleSetException("Match URL must be a non-empty value.");
            }

            var patternOptions = UrlRewriterManager.RuleOptions;

            if (ignoreCase)
            {
                ruleFlags.Add(new  NoCaseFlag());
                patternOptions |= RegexOptions.IgnoreCase;
            }

            return new Pattern(url, negate, patternOptions);
        }

        private enum LogicalGrouping
        {
            MatchAll = 0,
            MatchAny
        }

        /// <summary>
        /// Gets the conditions.
        /// </summary>
        /// <param name="conditionsElement">The conditions element.</param>
        /// <returns></returns>
        private IEnumerable<ICondition> GetConditions(XmlNode conditionsElement)
        {
            if (conditionsElement == null)
            {
                yield break;
            }

            var logicalGrouping = LogicalGrouping.MatchAll; // from schema definition
            var trackAllCaptures = false; // from schema definition

            if (conditionsElement.Attributes["logicalGrouping"] != null)
            {
                try { logicalGrouping = (LogicalGrouping)Enum.Parse(typeof(LogicalGrouping), conditionsElement.Attributes["logicalGrouping"].Value, true); }
                catch (Exception exc)
                {
                    Logger.Error("Condition: " + exc.Message, exc);
                }
            }

            if (conditionsElement.Attributes["trackAllCaptures"] != null)
            {
                trackAllCaptures = XmlConvert.ToBoolean(conditionsElement.Attributes["trackAllCaptures"].Value);
            }

            // process each condition
            foreach (XmlNode conditionElement in conditionsElement.SelectNodes("add"))
            {
                yield return GetCondition(conditionElement, logicalGrouping);
            }
        }

        private enum MatchType
        {
            Pattern = 0,
            IsFile,
            IsDirectory
        }

        /// <summary>
        /// Gets the condition.
        /// </summary>
        /// <param name="conditionElement">The condition element.</param>
        /// <param name="matchAll">if set to <see langword="true"/> [match all].</param>
        /// <returns></returns>
        private ICondition GetCondition(XmlNode conditionElement, LogicalGrouping logicalGrouping)
        {
            var input = "-";
            var matchType = MatchType.Pattern; // from schema definition
            var pattern = "(.*)";
            var ignoreCase = true; // from schema definition
            var negate = false; // from schema definition

            if (conditionElement.Attributes["input"] != null)
            {
                input = conditionElement.Attributes["input"].Value;
            }

            if (conditionElement.Attributes["matchType"] != null)
            {
                try { matchType = (MatchType)Enum.Parse(typeof(MatchType), conditionElement.Attributes["matchType"].Value, true); }
                catch (Exception exc)
                {
                    Logger.Error("Condition: " + exc.Message, exc);
                }
            }

            if (conditionElement.Attributes["pattern"] != null)
            {
                pattern = conditionElement.Attributes["pattern"].Value;
            }

            if (conditionElement.Attributes["ignoreCase"] != null)
            {
                ignoreCase = XmlConvert.ToBoolean(conditionElement.Attributes["ignoreCase"].Value);
            }

            if (conditionElement.Attributes["negate"] != null)
            {
                negate = XmlConvert.ToBoolean(conditionElement.Attributes["negate"].Value);
            }

            var conditionOptions = UrlRewriterManager.RuleOptions;
            IConditionFlagProcessor conditionFlags = new ConditionFlagProcessor();

            if (ignoreCase)
            {
                conditionFlags.Add(new UrlRewriter.Conditions.Flags.NoCaseFlag());
                conditionOptions |= RegexOptions.IgnoreCase;
            }

            if (logicalGrouping == LogicalGrouping.MatchAny)
            {
                conditionFlags.Add(new OrNextFlag());
            }

            ICondition condition = null;

            // create the condition
            switch (matchType)
            {
                case MatchType.IsFile: condition = new IsFileCondition(); break;
                case MatchType.IsDirectory: condition = new IsDirectoryCondition(); break;
                case MatchType.Pattern: condition = new DefaultCondition("", ""); break;
            }

            var compiledPattern = new Pattern(pattern, negate, conditionOptions);
            var conditionTest = new DefaultConditionTestValue(input);

            // initialize condition
            condition.Init(compiledPattern, conditionTest, conditionFlags);

            return condition;
        }
    }
}
