using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Grocery
{
    class Grocery
    {

        public static DataTable customers;
        public static DataTable registers;
        public static int registerCount;
        public static string file;

        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the file name");
            ReadInputFile();
            EndApplication();
        } // End of Main

        static void EndApplication()
        {
            Console.ReadKey();
        } // End of EndApplication

        static void ReadInputFile()
        {
            // Declare local variables
            int cnt = 1; // counter to capture register count from first line
            string line = string.Empty; // string for input line
            customers = new DataTable(); // customers table

            // Set columns for customers table
            customers.Columns.Add("CustomerType", typeof(String));
            customers.Columns.Add("StartTime", typeof(Int32));
            customers.Columns.Add("ItemCount", typeof(Int32));
            customers.Columns.Add("AssignedRegister", typeof(Int32));
            customers.Columns.Add("AssignOrder", typeof(Int32));
            customers.Columns.Add("TimeMultiplier", typeof(Int32));
            customers.Columns.Add("TimeCompleted", typeof(Int32));
            
            // Capture input file name
            file = Console.ReadLine();

            try
            {
                // Open input file 
                StreamReader sr = new StreamReader(file);

                // Iterate through file
                while ((line = sr.ReadLine()) != null)
                {
                    // Set registerCount
                    if (cnt == 1)
                    {
                        CreateRegistersTable(int.Parse(line.ToString().Trim()));
                        cnt++;
                    }
                    else
                    {
                        if (line.Trim().Length > 4)
                        {
                            // Parse string and write to customers Table
                            string[] words = new string[3];
                            char d = ' ';
                            words = line.Split(d);

                            customers.Rows.Add(words[0], words[1], words[2], 0, 0, 0, 0);
                        } // End of line trim length nested if structure
                    } // End of if structure
                } // End of while

                // Close StreamReader
                sr.Close();

                // Sort DataTable
                DataView dv = customers.DefaultView;
                dv.Sort = "StartTime, ItemCount, CustomerType";
                customers = dv.ToTable();

                ProcessAllCustomers();
                ProduceOutput();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error was encountered processing the file:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace.ToString());
                Console.WriteLine("Please examine input file to resolve this error.");
                EndApplication();
                            } // End of try catch
        } // End of ReadInputFile


        static void CreateRegistersTable(int cnt)
        {
                // instantiate registers table
                registers = new DataTable(); // registers table

                // Set columns for registers table
                registers.Columns.Add("RegisterNumber", typeof(Int32));
                registers.Columns.Add("TimeMultiplier", typeof(Int32));
                registers.Columns.Add("TotalCustomers", typeof(Int32));
                registers.Columns.Add("TotalItemCount", typeof(Int32));
                registers.Columns.Add("LastCustomerItemCount", typeof(Int32));
                registers.Columns.Add("CurrentTime", typeof(Int32));
                registers.Columns.Add("CompletionTime", typeof(Int32));

                // Add registers to registers table and set last register to double the multiplier
                for (int c = 1; c <= cnt; c++)
                    if (c == cnt)
                        registers.Rows.Add(c, 2, 0, 0, 0, 0, 0);
                    else
                        registers.Rows.Add(c, 1, 0, 0, 0, 0, 0);
        } // End of CreateRegistersTable


        static void ProcessAllCustomers()
        {
                // Declare local variables
                int currentTime = 0; // current time
                int maxTime = int.Parse(customers.Rows[customers.Rows.Count - 1]["StartTime"].ToString());

            // iterate through customers and process by assigning register and calculating register table values
                while (currentTime <= maxTime)
                {
                    // Clear out all processed customers
                    ClearOutProcessedCustomers(currentTime);
                    
                    // Compare currentTime to StartTime values and assign register for all matching records
                    foreach (DataRow dr in customers.Rows)
                    {
                        if (int.Parse(dr["StartTime"].ToString()) == currentTime)
                        {
                            // Determine which register to assign to customer
                            int assignedRegister = 0; // variable for AssignedRegister value
                            int timeMultiplier = 0; // variable to determine time per item needed to process items
                            int timeCompleted = 0; // variable for estimated time of completion of processing
                            int lastCustomerItemCount = int.MaxValue; // variable for counting LastCustomerItemCount for CustomerType B
                            int lowestCustomerCount = int.MaxValue; // variable to store lowest number of customers in line
                            bool updateCustomer = true; // Flag to update customer if no open registers
                            int regA = -1;

                            for (int reg = 0; reg < registers.Rows.Count; reg++)
                            {
                                // Determine if register has 0 customers
                                if (int.Parse(registers.Rows[reg]["TotalCustomers"].ToString()) == 0)
                                {
                                    // register has 0 customers in line - assign register
                                    regA = reg; //  int.Parse(registers.Rows[reg]["RegisterNumber"].ToString());

                                    // Escape for loop
                                    break;
                                } // End of 0 customers if structure
                            } // End of for loop

                                if(regA >= 0)
                                {
                                    // Assign customer to regA - empty register
                                    dr["AssignedRegister"] = regA + 1;
                                    dr["AssignOrder"] = 1;
                                    dr["TimeMultiplier"] = int.Parse(registers.Rows[regA]["TimeMultiplier"].ToString());
                                    dr["TimeCompleted"] = ((int.Parse(registers.Rows[regA]["TimeMultiplier"].ToString()) * int.Parse(dr["ItemCount"].ToString())) + currentTime);
                                    updateCustomer = false;
                                    registers.Rows[regA]["TotalCustomers"] = 1;

                                    // Update registers CompletionTime value
                                    UpdateRegistersCompletionTime();
                                }
                                else
                            {
                                    // Determine register to assign to customer
                            for (int reg = 0; reg < registers.Rows.Count; reg++)
                            {
                                    // Determine if CustomerType is A or B
                                    if (dr["CustomerType"].ToString().ToUpper() == "A")
                                    {
                                        // Select register with fewest number of customers
                                        if (int.Parse(registers.Rows[reg]["TotalCustomers"].ToString()) < lowestCustomerCount)
                                        {
                                            lowestCustomerCount = int.Parse(registers.Rows[reg]["TotalCustomers"].ToString());
                                            assignedRegister = reg + 1;
                                            timeMultiplier = int.Parse(registers.Rows[reg]["TimeMultiplier"].ToString());
                                            timeCompleted = ((int.Parse(registers.Rows[reg]["TimeMultiplier"].ToString()) * int.Parse(dr["ItemCount"].ToString())) + int.Parse(registers.Rows[reg]["CompletionTime"].ToString()));
                                        } // End of nested lowestCustomerCount if structure
                                    }
                                    else
                                    {
                                        // Select register with customer last in line that has fewest items
                                        if (int.Parse(registers.Rows[reg]["LastCustomerItemCount"].ToString()) < lastCustomerItemCount)
                                        {
                                            lastCustomerItemCount = int.Parse(registers.Rows[reg]["LastCustomerItemCount"].ToString());
                                            assignedRegister = reg + 1;
                                            timeMultiplier = int.Parse(registers.Rows[reg]["TimeMultiplier"].ToString());
                                            timeCompleted = ((int.Parse(registers.Rows[reg]["TimeMultiplier"].ToString()) * int.Parse(dr["ItemCount"].ToString())) + int.Parse(registers.Rows[reg]["CompletionTime"].ToString()));
                                        } // End of nested lastCustomerItemCount  if structure
                                    } // End of CustomerType if structure
                                } // End of outer customers in line = 0 if structure
                            } // End of for loop

                            // Update values if not updated
                            if (updateCustomer)
                            {
                                dr["AssignedRegister"] = assignedRegister;
                                dr["TimeMultiplier"] = timeMultiplier;
                                dr["TimeCompleted"] = timeCompleted;
                                dr["AssignOrder"] = GetAssignOrder(assignedRegister);
                                
                                // Update registers CompletionTime value
                                UpdateRegistersCompletionTime();
                            } // End of updateCustomer if structure
                        } // End of time matches if structure
                    } // End of foreach - customers.Rows

                    // Increment currentTime
                    currentTime++;
                } // End of while loop
        } // End of ProcessAllCustomers


static void ClearOutProcessedCustomers(int currentTime)
{
    // Iterate through customers table and delete any rows that have been completely processed
    for(int cnt = 0; cnt < customers.Rows.Count; cnt++)
        if ((int.Parse(customers.Rows[cnt]["TimeCompleted"].ToString()) <= currentTime) && (int.Parse(customers.Rows[cnt]["TimeCompleted"].ToString()) != 0))
        customers.Rows[cnt].Delete();

    UpdateRegistersCompletionTime();
} // End of ClearOutProcessedCustomers


static int GetAssignOrder(int reg)
{
    // Declare local variables
    int maxAssignedOrder  = 0;

    // Iterate through rows and capture maxCount of AssignOrder for customers in line
    foreach (DataRow dr in customers.Rows)
        if(int.Parse(dr["AssignedRegister"].ToString()) == reg)
        if (int.Parse(dr["AssignOrder"].ToString()) >= maxAssignedOrder)
            maxAssignedOrder = (int.Parse(dr["AssignOrder"].ToString()) + 1);

    return maxAssignedOrder;
} // End of GetAssignOrder


static void UpdateRegistersCompletionTime()
{
    // Iterate through registers table and update values
    for (int i = 1; i <= registers.Rows.Count; i++)
    {
        int custCount = 0;
        int maxAssignedOrder = 0;
        int LastCustItemCount = 0;
        int timeCompleted = 0;

        // Iterate through rows and capture number of customers, item count of last customer in line
        foreach (DataRow dr in customers.Select("AssignedRegister = " + i))
        {
            // Increment customer count
            custCount++;

            if (int.Parse(dr["AssignOrder"].ToString()) > maxAssignedOrder)
            {
                maxAssignedOrder = int.Parse(dr["AssignOrder"].ToString());
                LastCustItemCount = int.Parse(dr["ItemCount"].ToString());
                timeCompleted = int.Parse(dr["TimeCompleted"].ToString());
            } // End of if structure
        } // End of foreach

        // Update values for registers DataRow
        registers.Rows[i - 1]["TotalCustomers"] = custCount;
        registers.Rows[i - 1]["LastCustomerItemCount"] = LastCustItemCount;
        registers.Rows[i - 1]["CompletionTime"] = timeCompleted;
    } // End of for loop
} // End of UpdateRegistersCompletionTime


static void ProduceOutput()
{
    // Declare local variables
    int maxTime = 0;

    // Iterate through registers and capture greatest CompletionTime value
    foreach (DataRow dr in registers.Rows)
        if(int.Parse(dr["CompletionTime"].ToString()) > maxTime) maxTime = int.Parse(dr["CompletionTime"].ToString()); 

        Console.WriteLine("c:\\>grocery.exe " + file);
        Console.WriteLine("Finished at: t=" + maxTime.ToString() + "minutes");
} // End of ProduceOutput




    } // End of Class
} // End of Namespace
