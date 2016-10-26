using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace SoftwareSecurity
{
	/// <summary>
	/// Detects when windows date/time is not valid
	/// </summary>
	class DateTimeMonitor
	{
		const string SYSTEM_VARIABLE = "LastDateTime";
		string[] fileNames;

		public bool IsDateTimeValid
		{
			get 
			{
				DateTime now = DateTime.Now;
				
				foreach (string fileName in fileNames)
					if (!CheckFileModification(now, fileName))
						return false;

				return CheckEventLog(now) && CheckSystemVariable(now);
			}
		}

		public DateTimeMonitor(params string[] fileNames)
		{
			this.fileNames = fileNames;
		}

		public bool CheckFileModification(DateTime now, string fileName)
		{
			if (!File.Exists(fileName))
				return true;

			FileInfo info = new FileInfo(fileName);
			DateTime dateTo = info.LastAccessTime;
			info.LastAccessTime = now;
			return now > dateTo;
		}

		public bool CheckEventLog(DateTime now)
		{
			EventLog log = new EventLog("System");
			DateTime dateTo = log.Entries[log.Entries.Count - 1].TimeWritten;
			return now > dateTo;
		}

		public bool CheckSystemVariable(DateTime now)
		{
			string lastDateTime = Environment.GetEnvironmentVariable(SYSTEM_VARIABLE, EnvironmentVariableTarget.Machine);
			if (lastDateTime == null)
			{
				lastDateTime = DateTime.Now.Ticks.ToString();
				Environment.SetEnvironmentVariable(SYSTEM_VARIABLE, lastDateTime, EnvironmentVariableTarget.Machine);
				return true;
			}

			long ticks;
			if (long.TryParse(lastDateTime, out ticks))
			{
				DateTime dateTo = new DateTime(ticks);
				return now > dateTo;
			}
			else 
				return false;
		}
	}
}
