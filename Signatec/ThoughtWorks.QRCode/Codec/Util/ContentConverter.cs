using System;
namespace ThoughtWorks.QRCode.Codec.Util
{
	public class ContentConverter
	{
		internal static char n = '\n';
		public static string convert(string targetString)
		{
			string result;
			if (targetString == null)
			{
				result = targetString;
			}
			else
			{
				if (targetString.IndexOf("MEBKM:") > -1)
				{
					targetString = ContentConverter.convertDocomoBookmark(targetString);
				}
				if (targetString.IndexOf("MECARD:") > -1)
				{
					targetString = ContentConverter.convertDocomoAddressBook(targetString);
				}
				if (targetString.IndexOf("MATMSG:") > -1)
				{
					targetString = ContentConverter.convertDocomoMailto(targetString);
				}
				if (targetString.IndexOf("http\\://") > -1)
				{
					targetString = ContentConverter.replaceString(targetString, "http\\://", "\nhttp://");
				}
				result = targetString;
			}
			return result;
		}
		private static string convertDocomoBookmark(string targetString)
		{
			targetString = ContentConverter.removeString(targetString, "MEBKM:");
			targetString = ContentConverter.removeString(targetString, "TITLE:");
			targetString = ContentConverter.removeString(targetString, ";");
			targetString = ContentConverter.removeString(targetString, "URL:");
			return targetString;
		}
		private static string convertDocomoAddressBook(string targetString)
		{
			targetString = ContentConverter.removeString(targetString, "MECARD:");
			targetString = ContentConverter.removeString(targetString, ";");
			targetString = ContentConverter.replaceString(targetString, "N:", "NAME1:");
			targetString = ContentConverter.replaceString(targetString, "SOUND:", ContentConverter.n + "NAME2:");
			targetString = ContentConverter.replaceString(targetString, "TEL:", ContentConverter.n + "TEL1:");
			targetString = ContentConverter.replaceString(targetString, "EMAIL:", ContentConverter.n + "MAIL1:");
			targetString += ContentConverter.n;
			return targetString;
		}
		private static string convertDocomoMailto(string s)
		{
			char c = '\n';
			string text = ContentConverter.removeString(s, "MATMSG:");
			text = ContentConverter.removeString(text, ";");
			text = ContentConverter.replaceString(text, "TO:", "MAILTO:");
			text = ContentConverter.replaceString(text, "SUB:", c + "SUBJECT:");
			text = ContentConverter.replaceString(text, "BODY:", c + "BODY:");
			return text + c;
		}
		private static string replaceString(string s, string s1, string s2)
		{
			string text = s;
			for (int i = text.IndexOf(s1, 0); i > -1; i = text.IndexOf(s1, i + s2.Length))
			{
				text = text.Substring(0, i) + s2 + text.Substring(i + s1.Length);
			}
			return text;
		}
		private static string removeString(string s, string s1)
		{
			return ContentConverter.replaceString(s, s1, "");
		}
	}
}
