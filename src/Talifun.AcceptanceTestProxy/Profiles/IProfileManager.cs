using System;
using System.Net;

namespace Talifun.AcceptanceTestProxy.Profiles
{
    public interface IProfileManager
    {
        Uri HandleRequest(string profileName, HttpWebRequest request);
    }
}