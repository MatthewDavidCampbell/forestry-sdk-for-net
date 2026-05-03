namespace Forestry.PapiNet.Contract
{
    /// <summary>
    /// PapiNet contracts can be used by business partners to communicate business agreements.
    /// </summary>
    public class Contract
    {
        public ContractType ContractType { get; set; }

        public string ContractStatusType { get; set; }

        public string ContractContextType { get; set; }

        public string ContractValidityStatus { get; set; }

        public ContractHeader ContractHeader { get; set; }
    }
}