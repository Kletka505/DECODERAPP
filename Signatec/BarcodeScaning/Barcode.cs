namespace Signatec.BarcodeScaning
{
	/// <summary>
	/// ���� ���������� ���������
	/// </summary>
	public enum BarcodeSourceType
	{
		Default = 0,
		/// <summary>
		/// ������
		/// </summary>
		Higina = 1,
        /// <summary>
        /// ��������� �� �����
        /// </summary>
        AccountingMission = 2,
    }

    /// <summary>
    /// ���������� � ���� ���������, ������� � ���������.
    /// </summary>
    public class BarcodeDocField
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	/// <summary>
	/// ����������, ������� � ���������.
	/// </summary>
	public class Barcode
	{
		public BarcodeSourceType SourceType = BarcodeSourceType.Default;
		public BarcodeDocField[] Fields;
		public string Hash;
	}
}