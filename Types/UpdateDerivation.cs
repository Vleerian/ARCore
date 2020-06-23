using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

using Newtonsoft.Json;

namespace ARCore.Types
{
    /// <summary>
    /// A utility class contianing the lastupdates of two significant regions
    /// banana updates first, packer updates last. Using these, you can derive
    /// the update length.
    /// </summary>
    /// 
    public sealed class UpdateDerivation
    {
        public UpdateDerivation(int banana, int packer){
            BananaLastUpdate = banana;
            PackerLastUpdate = packer;
        }

        [JsonProperty("banana")]
        public int BananaLastUpdate;
        [JsonProperty("packer")]
        public int PackerLastUpdate;

        public int UpdateStart {
            get { return BananaLastUpdate; }
        }
        public int UpdateEnd {
            get { return PackerLastUpdate; }
        }
        public int UpdateLength {
            get { return PackerLastUpdate - BananaLastUpdate; }
        }
    }
}
