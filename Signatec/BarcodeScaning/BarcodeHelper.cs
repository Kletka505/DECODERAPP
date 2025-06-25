using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signatec.BarcodeScaning
{
	public class BarcodeHelper
	{
		readonly List<BarcodeDocField> _barcode = new List<BarcodeDocField>();

		public Barcode Barcode
		{
			get
			{
				return new Barcode { Fields = _barcode.ToArray() };
			}  
			set
			{
				_barcode.Clear();
				_barcode.AddRange(value.Fields);
			}
		}

		BarcodeDocField GetField(string fieldName)
		{
			var nameFields = _barcode.Where(f => f.Name.ToUpper() == fieldName.ToUpper());
			if(nameFields.Count()>0)
			{
				return nameFields.First();
			}
			var field = new BarcodeDocField() {Name = fieldName, Value = string.Empty};
			_barcode.Add(field);
			return field;
		}

		public void AddOrReplaceField(string fieldName, string fieldValue)
		{
			GetField(fieldName).Value = fieldValue;
		}

		//private const string TemplateIdFiledName = "TemplateId";

		//public Guid TemplateId
		//{
		//    get
		//    {
		//        BarcodeDocField fieled = GetField(TemplateIdFiledName);
		//        if (fieled.Value == string.Empty) throw new BarcodeException("TemplateId не задан");
		//        return Guid.Parse(fieled.Value);
		//    }
		//    set
		//    {
		//        BarcodeDocField fieled = GetField(TemplateIdFiledName);
		//        fieled.Value = value.ToString();
		//    }  
		//}

		//private const string SerialNumberFieldName = "SerialNumber";

		//public string SerialNumber
		//{
		//    get
		//    {
		//        BarcodeDocField fieled = GetField(SerialNumberFieldName);
		//        if (fieled.Value == string.Empty) throw new BarcodeException("SerialNumber не задан");
		//        return fieled.Value;
		//    }
		//    set
		//    {
		//        BarcodeDocField fieled = GetField(SerialNumberFieldName);
		//        fieled.Value = value.ToString();
		//    }
		//}
	}
}
