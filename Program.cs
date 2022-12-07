namespace Final_project;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;
using System.Data;
using MySql.Data.MySqlClient;
class Program{
        public static string connStr = "server=20.172.0.16;user=ajverner1;database=ajverner1;port=8080;password=ajverner1";

        // print login page
        public static Staff Login(){
            Staff staff = new Staff();
            Console.WriteLine("------Welcome to Package Management System------");
            Console.WriteLine("Please input user ID (StaffID): ");
            staff.staff_username = Console.ReadLine();
            Console.WriteLine("Please input password: ");
            staff.staff_password = Console.ReadLine();
            return staff;
        }
        public static void DisplayResident(DataTable tableResident){
            int idx = 0;
            Console.WriteLine("---------------Resident List-------------------");
            foreach(DataRow row in tableResident.Rows){
                Console.WriteLine($"{idx}: ResidentID: {row["id"]} \t Resident Unit # {row["unit_number"]} \t ResidentName: {row["full_name"]} \t Email:{row["email"]}");
                idx++;
            }
        }




    public static DataTable CheckResident(){
        MySqlConnection conn = new MySqlConnection(connStr);
        Console.WriteLine("Please input the Unit Number.");
        int unitNumber = Convert.ToInt16(Console.ReadLine());
        try
        {  
            conn.Open();
            string procedure = "CheckResident";
            MySqlCommand cmd = new MySqlCommand(procedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@inputUnitNumber", unitNumber);
            cmd.Parameters["@inputUnitNumber"].Direction = ParameterDirection.Input;
            
            MySqlDataReader rdr = cmd.ExecuteReader();

            DataTable tableResident = new DataTable();
            tableResident.Load(rdr);
            rdr.Close();
            conn.Close();
            return tableResident;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            conn.Close();
            return null;
        }
    }

    static async Task Main(string[] args)
    {
        Staff staff = Login();
        bool valid = LoginCheck(staff);
        string target_resident_email = string.Empty;

        while(valid){
            int option = Dashboard(staff);
            switch(option){
                //Search Resident for package
                case 1:
                    DataTable tableResident = CheckResident();
                    DisplayResident(tableResident);
                    Console.WriteLine("Please input the index (0 or 1) to select target resident");
                    int idx = Convert.ToInt16(Console.ReadLine());
                    target_resident_email = tableResident.Rows[idx]["email"].ToString();
                    Console.WriteLine($"An email notification will be sent to: {target_resident_email}");
                    break;
                //Send Package Email
                case 2: 
                        string serviceConnectionString =  "endpoint=https://ajvernercommunicationservice.communication.azure.com/;accesskey=2G16ooyM2zz/tMDcvsdoFxZb78pK6f6ABIgWxkISbTUTvIrr2x3Lo/t57WIg4V9w+Hs+v8W9lS1oVZoqtMcirg==";
                        EmailClient emailClient = new EmailClient(serviceConnectionString);
                        var subject = "Package at the front desk.";
                        var emailContent = new EmailContent(subject);
                        // use Multiline String @ to design html content
                        emailContent.Html= @"
                                    <html>
                                        <body>
                                            <h1 style=color:red>Your Package has arived at the front desk</h1>
                                            <h4>At earliest convenianced come and pick it up.</h4>
                                            <p>Have a nice day</p>
                                        </body>
                                    </html>";


                        // mailfrom domain of your email service on Azure
                        var sender = "DoNotReply@5de205c2-dd33-4042-acaf-1355d17069d4.azurecomm.net";

                       
                        var emailRecipients = new EmailRecipients(new List<EmailAddress> {
                            new EmailAddress(target_resident_email) { DisplayName = "Testing" },
                        });

                        var emailMessage = new EmailMessage(sender, emailContent, emailRecipients);

                        try
                        {
                            SendEmailResult sendEmailResult = emailClient.Send(emailMessage);

                            string messageId = sendEmailResult.MessageId;
                            if (!string.IsNullOrEmpty(messageId))
                            {
                                Console.WriteLine($"Email sent, MessageId = {messageId}");
                            }
                            else
                            {
                                Console.WriteLine($"Failed to send email.");
                                return;
                            }

                            // wait max 2 minutes to check the send status for mail.
                            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                            do
                            {
                                SendStatusResult sendStatus = emailClient.GetSendStatus(messageId);
                                Console.WriteLine($"Send mail status for MessageId : <{messageId}>, Status: [{sendStatus.Status}]");

                                if (sendStatus.Status != SendStatus.Queued)
                                {
                                    break;
                                }
                                await Task.Delay(TimeSpan.FromSeconds(10));
                            
                            } while (!cancellationToken.IsCancellationRequested);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Console.WriteLine($"Looks like we timed out for email");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in sending email, {ex}");
                        }   
                        break;
                // Package Return
                case 3:
                        break;
                case 4:
                    Console.WriteLine($"Log out succesful, Have a great day {staff.staff_username}.");
                    valid = false;
                    break;
            }
        
            
            
        }
    }
    public static bool LoginCheck(Staff staff){
        MySqlConnection conn = new MySqlConnection(connStr);
        try
        {  
            conn.Open();
            string procedure = "LoginCount";
            MySqlCommand cmd = new MySqlCommand(procedure, conn);
            cmd.CommandType = CommandType.StoredProcedure; // set the commandType as storedProcedure
            cmd.Parameters.AddWithValue("@inputUsername", staff.staff_username);
            cmd.Parameters.AddWithValue("@inputPassword", staff.staff_password);
            cmd.Parameters.Add("@userCount", MySqlDbType.Int32).Direction =  ParameterDirection.Output;
            MySqlDataReader rdr = cmd.ExecuteReader();
            
            int returnCount = (int) cmd.Parameters["@userCount"].Value;
            rdr.Close();
            conn.Close();

            if (returnCount ==1){
                return true;
            }
            else{
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            conn.Close();
            return false;
        }
    }

        public static int Dashboard(Staff staff){
        DateTime localDate = DateTime.Now;
        Console.WriteLine("---------------Dashboard-------------------");
        Console.WriteLine($"Hello: {staff.staff_username}; Date/Time: {localDate.ToString()}");
        Console.WriteLine("Please select an option to continue:");
        Console.WriteLine("1. Search Resident for Package");
        Console.WriteLine("2. Send Email");
        Console.WriteLine("3. Package Return(TBI)");
        Console.WriteLine("4. Log Out");
        int option = Convert.ToInt16(Console.ReadLine());
        return option;
    }
}