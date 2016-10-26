using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace SoftwareSecurity
{
	class Protector
	{
		const string codeElement = "Code";
		const string hashElement = "Hash";
		const string signatureElement = "Signature";
		const string licenseFile = "license.xml";

		#region Secret constants

		// Global, binded to application
		long applicationCode = 0x00f3e9add014d458;
		string serverKey = "ai256@inbox.ru";

		// StringToUInt
		// int uintCode = 0x2D7A61BE;

		// StringToULong
		long longCode = 0x184E0786A514F0ff;

		// IsHardwareValid
		long iHardware = 0xA0AB24332CFEF5;
		long hardwareCode = 0x4fe5ea16786ac8be;

		// IsDateTimeValid
		long fromXor = 0x15FA83B08D758EAA;
		long toXor = 0x77AF210D9ABEC081;

		long fromCode = 0x1d33602c375fcfaa;
		long toCode = 0x7f6261d05644d781;

		long iDates = 0x6D22333C18E9FC;
		long dates = 0x623c23cf5d03f1d7;

		long iXors = 0x3E60BADF20C351;
		long xors = 0x626bc207c8eb8d7a; 

		#endregion

		#region Additional security
		
		DateTimeMonitor dateTimeMonitor;
		SelfTest selfTest;
		
		#endregion

		public bool IsValidVersion
		{
			get
			{
				long app = iHardware ^ iDates ^ iXors;

				if (app != applicationCode)
					return false;
				else if (!dateTimeMonitor.IsDateTimeValid)
					return false;
				else if (!selfTest.AssemblyIsValid)
					return false;
				else if (!IsHardwareValid())
					return false;
				else if (!IsDateTimeValid())
					return false;

				return true;
			}
		}

		public Protector(long applicationCode, string serverKey)
		{
			this.applicationCode = applicationCode;
			this.serverKey = serverKey;	
	
			// Initiate additional security
			string windowsDirectory = Environment.GetEnvironmentVariable("SystemRoot");
			dateTimeMonitor = new DateTimeMonitor(licenseFile);
		}

		public bool LoadLicense()
		{
			RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
			string publicKey = Encoding.ASCII.GetString(Convert.FromBase64String(serverKey));
			csp.FromXmlString(publicKey);

			// Load the signed XML license file.
			XmlDocument xmldoc = new XmlDocument();
			xmldoc.Load(licenseFile);

			// Create the signed XML object.
			SignedXml sxml = new SignedXml(xmldoc);
			byte[] code = null;

			try
			{
				// Get the XML Signature node and load it into the signed XML object.
				XmlNode dsig = xmldoc.GetElementsByTagName(signatureElement, SignedXml.XmlDsigNamespaceUrl)[0];
				sxml.LoadXml((XmlElement)dsig);		

				// Verify the signature.
				if (!sxml.CheckSignature(csp))
					return false;

				XmlNode node = xmldoc.GetElementsByTagName(codeElement)[0];
				code = Convert.FromBase64String(node.InnerText);

				node = xmldoc.GetElementsByTagName(hashElement)[0];
				selfTest = new SelfTest(node.InnerText);
			}
			catch { return false; }

			MemoryStream stream = new MemoryStream(code);
			BinaryReader reader = new BinaryReader(stream);

			longCode = reader.ReadInt64();
			iHardware = reader.ReadInt64();
			hardwareCode = reader.ReadInt64();
			fromXor = reader.ReadInt64();
			toXor = reader.ReadInt64();
			fromCode = reader.ReadInt64();
			toCode = reader.ReadInt64();
			iDates = reader.ReadInt64();
			dates = reader.ReadInt64();
			iXors = reader.ReadInt64();
			xors = reader.ReadInt64();

			reader.Close();
			stream.Close();
			return true;
		}

		long StringToLong(string input)
		{
			return (long)StringToULong(input);
		}

		ulong StringToULong(string input)
		{
			ulong result = (ulong)longCode; // VARIABLE

			switch (input.Length % 8)
			{
				case 1: input = input + "0123456"; break;
				case 2: input = input + "012345"; break;
				case 3: input = input + "01234"; break;
				case 4: input = input + "0123"; break;
				case 5: input = input + "012"; break;
				case 6: input = input + "01"; break;
				case 7: input = input + "0"; break;
			}

			byte[] bytes = ASCIIEncoding.ASCII.GetBytes(input);
			for (int i = 0; i < input.Length; i += 8)
			{
				ulong low = (ulong)((bytes[i] << 24) | (bytes[i + 1] << 16) | (bytes[i + 2] << 8) | (bytes[i + 3]));
				ulong high = (ulong)((bytes[i + 4] << 24) | (bytes[i + 5] << 16) | (bytes[i + 6] << 8) | (bytes[i + 7]));
				result ^= low | (high << 32);
			}

			return result;
		}

		public bool IsHardwareValid()
		{
			SysInfo sysInfo = new SysInfo();
			bool licensed = false;

			string cpu = sysInfo.GetCPUId();
			long _cpu = StringToLong(sysInfo.GetCPUId());

			foreach (string drive in Directory.GetLogicalDrives())
			{
				string volume = drive.Substring(0, 1);
				long _drive = StringToLong(sysInfo.GetVolumeSerial(volume).ToString("X"));
				long driveSerial = _drive ^ _cpu ^ iHardware;
				if (driveSerial == hardwareCode)
					licensed = true;
			}
			return licensed;
		}

		public bool IsDateTimeValid()
		{
			long ticks = DateTime.Now.Ticks;

			if (dates != (fromCode ^ toCode ^ iDates)) // Variable Date times
				return false;

			if (xors != (fromXor ^ toXor ^ iXors)) // Variable Xors
				return false;

			DateTime date_from = new DateTime(fromCode ^ fromXor);
			DateTime date_to = new DateTime(toCode ^ toXor);

			if (date_from.Ticks > ticks)
				return false;
			else if (date_to.Ticks < ticks)
				return false;
			else
				return true;
		}
	}
}
