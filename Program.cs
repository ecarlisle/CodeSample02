using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Net;
using System.IO;


namespace LogToCSV
{

	/* Hello,
	 * Thank you for your time and consideration in viewing this program.  In total time, this probably took somewhere between 5-6 
	 * hours.  I took some extra time to be certain that I was using efficient collections for storage and searching, efficient search 
	 * methods for checing for duplicate IP Addresses, and meaningful methodology of sorting the octet value of the IP addresses.  For 
	 * the sort, I converted each IP address into a unsigned 32-bit integer contained within a long.  Those were the values I sorted by 
	 * and I hope it's consistent with expectations.
	 * 
	 * I also made the assumption that you wanted this program to do very little with plugins and other tools.  For example, Microsoft 
	 * makes Log Parser 2.2, which makes quick work out of sorting and querying IPs.  I didn't use it for the reason mentioned already and 
	 * because the integration point is via COM only (not an Assembly reference that becomes packaged with the program).  Therefore, to 
	 * use such a tool assumes that a local install of that Log Parser 2.2 is present, which is definitely not a certainty.
	 * 
	 * Performance wise, the runtime can vary a bit but the best time I've seen is an 11-second runtime (times are written to console at runtime).
	 * I'll think of other ways to perform performance, as I wasn't sure to make of expectations.
	 * 
	 * FYI, the CSV files are contained within the project in bin/DEBUG/csv and bin/release/csv.
	 * 
	 * Thank you for you time and consideration!
	 * 
	 * Best,
	 * Eric
	 * 
	 */

	class Program
	{
		static void Main(string[] args)
		{
			// Output the start time.
			DateTime startTime = DateTime.Now;
			Console.WriteLine("Program Start Time: " + startTime.ToString("T"));

			// Read all log file lines into an array.
			string[] lines = System.IO.File.ReadAllLines(@"logs/access.log");
			
			// Build a data table for all the lines we decide to keep.
			DataTable dt = new DataTable();
			dt.Columns.Add("Count", typeof(long));
			dt.Columns.Add("IP_Address", typeof(string));
			dt.Columns.Add("IP_Address_Integer", typeof(long));

			// Make the IP_Address Column a primary key to speed up the process of searching the data table over and over.
			// Note: the DataTable type does not have the capability to be indexed.
			dt.PrimaryKey = new DataColumn[] { dt.Columns["IP_Address"] };


			// Iterate over the lines array.  Since this iteration will be expensive, we want to hunt out bad lines and append the good lines into a dataTable.
			for (int i=0; i<lines.Length; i++){

				// Create array for that line, splitting fields by sspaces.  From this point, much of our conditional logic will be specific array indexes.
				// This assumes that this program is only for schema used in the logs/access.log file.
				string[] lineArray = lines[i].Split(' ');

				// We don't want to use comment lines or data within the comment lines.  To avoid this, we'll assume a length of 21 items for lines[i].
				if (lines[i].Substring(0, 1) != "#" && lineArray.Length == 21) {
					
					// Isolate lines where the request was a GET protocol on port 80. Also eliminate IPs starting with 207.114 .
					if (lineArray[7] == "80" && lineArray[8] == "GET" && lineArray[2].Substring(0,7) != "207.114") {


						// Create datarow to add to data table container.
						DataRow dr = dt.NewRow();
						dr["Count"] = 1;
						dr["IP_Address"] = lineArray[2];
						dr["IP_Address_Integer"] = IPtoInt(lineArray[2]);
						
						// Create duplicate search expression and check for duplicates.
						string searchExpression = "IP_Address = '" + lineArray[2].ToString() + "'";
						DataRow[] duplicateRow = dt.Select(searchExpression); 

						// Prevent duplicate rows for an IP address.  If a duplicate is fount, add 1 to the "Count" row.  Else, add the row.					
						if (duplicateRow.Length > 0) {
							int duplicateIndex = dt.Rows.IndexOf(duplicateRow[0]);
							dt.Rows[duplicateIndex]["Count"] = int.Parse(dt.Rows[duplicateIndex]["Count"].ToString()) + 1;
						} else {
							dt.Rows.Add(dr);
						}

						// Have the data table accept all changes.
						dt.AcceptChanges();

					}
				}
			}

			// Now sort the datatable by the IP Address integer representation.
			DataView dv = dt.DefaultView;
			dv.Sort = "Count desc, IP_Address_Integer desc";
			dt = dv.ToTable();

			// Create a string builder to contain the CSV file contents.
			StringBuilder sb = new StringBuilder();

			// Add column names as the first line.
			sb.Append("Count,IP_Address");
			//sb.Append("Count,IP_Address,IP_Address_Integer\n");
			

			// Add the data to subsequent lines
			foreach (DataRow row in dt.Rows) {
				var fields = row["Count"] + ",\"" + row["IP_Address"] + "\"\n";
				//var fields = row["Count"] + ",\"" + row["IP_Address"] + "\"," + row["IP_Address_Integer"];
				sb.AppendLine(fields);
			}

			// Write the CSV file to the file system.
			using (StreamWriter sw = new StreamWriter(@"csv\ip_address_count.csv")) {
				sw.Write(sb.ToString());
			}

			// Output the end time.
			DateTime endTime = DateTime.Now;
			Console.WriteLine("Program End Time: " + endTime.ToString("T"));

			// Output the start time.
			TimeSpan duration = endTime - startTime;
			Console.WriteLine("Program Duration: " + duration.Seconds.ToString() + " seconds");

			return;

		}

		// Converts the octal representation of an IP address to an unsigned integer (contained in a long).
		static long IPtoInt(string addr) {

			// Note: The "Address" property is deprecated bevause I believe it is only for IPV4 addresses only.
			// In this case, however, it makes a very good tool. 
			return (long)(uint)IPAddress.NetworkToHostOrder((int)IPAddress.Parse(addr).Address);
		}


	}
}
