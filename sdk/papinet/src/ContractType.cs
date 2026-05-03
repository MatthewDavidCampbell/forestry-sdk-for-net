namespace Forestry.PapiNet.Contract
{
    /// <summary>
    /// 
    /// </summary>
    public enum ContractType
    {
        /// <summary>
        /// A contract between the first seller and the first buyer in a supply chain.
        /// </summary>
        Original,

        /// <summary>
        /// A contract between a seller and a buyer, when products 
        /// (or services specified as products) bought by the seller are resold to a buyer in a supply chain.
        /// </summary>
        Trading
    }
}