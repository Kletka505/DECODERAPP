namespace Signatec.BarcodeScaning
{
	/// <summary>
	/// Типы источников штрихкода
	/// </summary>
	public enum BarcodeSourceType
	{
		Default = 0,
		/// <summary>
		/// Хижина
		/// </summary>
		Higina = 1,
        /// <summary>
        /// Поручение из Учёта
        /// </summary>
        AccountingMission = 2,
    }

    /// <summary>
    /// Информация о поле документа, зашитая в штрихкоде.
    /// </summary>
    public class BarcodeDocField
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	/// <summary>
	/// Информация, зашитая в штрихкоде.
	/// </summary>
	public class Barcode
	{
		public BarcodeSourceType SourceType = BarcodeSourceType.Default;
		public BarcodeDocField[] Fields;
		public string Hash;
	}
}