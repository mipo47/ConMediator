using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Management;

namespace SoftwareSecurity
{
	public class SysInfo
	{
		[DllImport("kernel32.dll")]
		private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);	
		
		/// <summary>
		/// Get Volume Serial Number as string
		/// </summary>
		/// <param name="strDriveLetter">Single letter (e.g., "C")</param>
		/// <returns>string representation of Volume Serial Number</returns>
		public uint GetVolumeSerial(string strDriveLetter)
		{
			uint serNum = 0;
			uint maxCompLen = 0;
			StringBuilder VolLabel = new StringBuilder(256);	// Label
			UInt32 VolFlags = new UInt32();
			StringBuilder FSName = new StringBuilder(256);	// File System Name
			strDriveLetter += ":\\";
			long Ret = GetVolumeInformation(strDriveLetter, VolLabel, (UInt32)VolLabel.Capacity, ref serNum, ref maxCompLen, ref VolFlags, FSName, (UInt32)FSName.Capacity);
			return serNum;
		}

		/// <summary>
		/// Return processorId from first CPU in machine
		/// </summary>
		/// <returns>[string] ProcessorId</returns>
		public string GetCPUId()
		{
			string cpuInfo = String.Empty;
			string temp = String.Empty;
			ManagementClass mc = new ManagementClass("Win32_Processor");
			ManagementObjectCollection moc = mc.GetInstances();
			foreach (ManagementObject mo in moc)
			{
				if (cpuInfo == String.Empty)
				{// only return cpuInfo from first CPU
					cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
				}
			}
			return cpuInfo;
		}
	}
}
