using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Threading.Tasks;

using ARCore.Core;

namespace ARCore
{
    public enum TelegramType
    {
        Recruitment,
        NonRecruitment
    }

    public class TelegramRequest
    {
        public string Uri => string.Format(TGEndpoint, ClientKey, TemplateID, SecretKey, Recipient);
        private const string TGEndpoint = "https://www.nationstates.net/cgi-bin/api.cgi?a=sendTG&client={0}&tgid={1}&key={2}&to={3}";
        public readonly string Recipient;
        public readonly string TemplateID;
        private readonly string ClientKey;
        private readonly string SecretKey;
        public readonly TelegramType Type;
        
        public TelegramRequest(TelegramType type, string recipient, string templateID, string clientKey, string secretKey)
        {
            Type = type;
            Recipient = recipient;
            TemplateID = templateID;
            ClientKey = clientKey;
            SecretKey = secretKey;
        }
    }
}
