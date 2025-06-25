using System;
using System.Collections.Generic;

namespace Signatec.BarcodeScaning
{
	//internal class ProcessConnection
	//{
	//	public static ConnectionOptions ProcessConnectionOptions()
	//	{
	//		ConnectionOptions options = new ConnectionOptions();
	//		options.Impersonation = ImpersonationLevel.Impersonate;
	//		options.Authentication = AuthenticationLevel.Default;
	//		options.EnablePrivileges = true;
	//		return options;
	//	}

	//	public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
	//	{
	//		ManagementScope connectScope = new ManagementScope();
	//		connectScope.Path = new ManagementPath(@"\\" + machineName + path);
	//		connectScope.Options = options;
	//		connectScope.Connect();
	//		return connectScope;
	//	}
	//}

	public class SerialPortInfo
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public override string ToString()
		{
			return string.Format("{0} – {1}", Name, Description);
		}

		public static IEnumerable<SerialPortInfo> GetSerialPortsInfo()
		{
		    throw new NotImplementedException();
			//List<SerialPortInfo> comPortInfoList = new List<SerialPortInfo>();

			//ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
			//ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

			//ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
			//ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

			//using (comPortSearcher)
			//{
			//	string caption = null;
			//	foreach (ManagementObject obj in comPortSearcher.Get())
			//	{
			//		if (obj != null)
			//		{
			//			object captionObj = obj["Caption"];
			//			if (captionObj != null)
			//			{
			//				caption = captionObj.ToString();
			//				if (caption.Contains("(COM"))
			//				{
			//					SerialPortInfo comPortInfo = new SerialPortInfo();
			//					comPortInfo.Name = caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty);
			//					comPortInfo.Description = caption;
			//					comPortInfoList.Add(comPortInfo);
			//				}
			//			}
			//		}
			//	}
			//}
			//return comPortInfoList;
		}
	}
}
