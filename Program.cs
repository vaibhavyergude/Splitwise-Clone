using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SplitwiseApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Splitwise Console App!");

            SplitwiseApp splitwiseApp = new SplitwiseApp();
            splitwiseApp.Run();
        }
    }

    class SplitwiseApp
    {
        private string connectionString = "Server=localhost;Port=3306;Database=dotnet;Uid=vaibhav;Pwd=cdac;";
        private string currentUser = "";

        public void Run()
        {
            while (true)
            {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Signup");
                Console.WriteLine("3. Exit");

                try
                {
                    int choice = GetValidChoice(1, 3);

                    switch (choice)
                    {
                        case 1:
                            if (Login())
                            {
                                MainMenu();
                            }
                            else
                            {
                                Console.WriteLine("Invalid credentials. Please try again.");
                            }
                            break;
                        case 2:
                            Signup();
                            break;
                        case 3:
                            Console.WriteLine("Exiting Splitwise Console App. Goodbye!");
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private void MainMenu()
        {
            ExpenseManager expenseManager = new ExpenseManager(connectionString, currentUser);

            while (true)
            {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1. Add an expense");
                Console.WriteLine("2. View expenses");
                Console.WriteLine("3. Update expense");
                Console.WriteLine("4. Delete expense");
                Console.WriteLine("5. Logout");

                try
                {
                    int choice = GetValidChoice(1, 5);

                    switch (choice)
                    {
                        case 1:
                            expenseManager.AddExpense();
                            break;
                        case 2:
                            expenseManager.ViewExpenses();
                            break;
                        case 3:
                            expenseManager.UpdateExpense();
                            break;
                        case 4:
                            expenseManager.DeleteExpense();
                            break;
                        case 5:
                            Console.WriteLine("Logged out successfully.");
                            currentUser = "";
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private bool Login()
        {
            UserAuthentication userAuthentication = new UserAuthentication(connectionString);
            Console.Write("Enter your username: ");
            string username = Console.ReadLine();

            Console.Write("Enter your password: ");
            string password = Console.ReadLine();

            if (userAuthentication.AuthenticateUser(username, password))
            {
                currentUser = username;
                return true;
            }

            return false;
        }

        private bool IsAlphanumeric(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }

        private string GetNonEmptyUserInput()
        {
            string input;
            do
            {
                input = Console.ReadLine();
            } while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input));
            return input.Trim();
        }

        private void Signup()
        {
            UserAuthentication userAuthentication = new UserAuthentication(connectionString);

            Console.Write("Enter a username: ");
            string username = GetNonEmptyUserInput();

            string password;
            do
            {
                Console.Write("Enter a password (at least 6 characters and alphanumeric): ");
                password = Console.ReadLine();
            } while (string.IsNullOrEmpty(password) || password.Length < 6 || !IsAlphanumeric(password));

            userAuthentication.SignupUser(username, password);
            Console.WriteLine("Signup successful! Please log in with your credentials.");
        }


        private int GetValidChoice(int minValue, int maxValue)
        {
            int choice;
            while (!int.TryParse(Console.ReadLine(), out choice) || choice < minValue || choice > maxValue)
            {
                Console.WriteLine($"Invalid input. Please enter a number between {minValue} and {maxValue}: ");
            }
            return choice;
        }
    }

    class UserAuthentication
    {
        private string connectionString;

        public UserAuthentication(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public bool AuthenticateUser(string username, string password)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count > 0;
                }
            }
        }

        public void SignupUser(string username, string password)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("INSERT INTO Users (Username, Password) VALUES (@Username, @Password)", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.ExecuteNonQuery();
                }


                UpdateBalance(connection, username, 0);
            }
        }

        private void UpdateBalance(MySqlConnection connection, string username, double amount)
        {
            using (MySqlCommand selectCommand = new MySqlCommand("SELECT Amount FROM Balances WHERE Username = @Username", connection))
            {
                selectCommand.Parameters.AddWithValue("@Username", username);
                object result = selectCommand.ExecuteScalar();

                if (result != null && double.TryParse(result.ToString(), out double currentBalance))
                {
                    double newBalance = currentBalance + amount;

                    using (MySqlCommand updateCommand = new MySqlCommand("UPDATE Balances SET Amount = @Amount WHERE Username = @Username", connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Amount", newBalance);
                        updateCommand.Parameters.AddWithValue("@Username", username);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                else
                {

                    using (MySqlCommand insertCommand = new MySqlCommand("INSERT INTO Balances (Username, Amount) VALUES (@Username, @Amount)", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Username", username);
                        insertCommand.Parameters.AddWithValue("@Amount", amount);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    class ExpenseDetails
    {
        public int Id { get; set; }
        public string FriendName { get; set; }
        public double Amount { get; set; }
        public DateTime Date { get; set; }
        public string ExpenseType { get; set; }
    }
    class ExpenseManager
    {
        private string connectionString;
        private string currentUser;

        public ExpenseManager(string connectionString, string currentUser)
        {
            this.connectionString = connectionString;
            this.currentUser = currentUser;
        }



        public void AddExpense()
        {
            if (currentUser == "")
            {
                Console.WriteLine("You must log in first to add an expense.");
                return;
            }

            Console.Write("Enter the total amount of the expense: ");
            double totalAmount = GetValidAmount();

            if (totalAmount <= 0)
            {
                Console.WriteLine("Total amount must be greater than zero. Expense not added.");
                return;
            }

            Console.Write("Enter the number of friends to split the expense with including you (maximum 5): ");
            int numberOfFriends = GetValidChoice(1, 5);

            List<string> friendNames = new List<string>();
            Dictionary<string, double> friendExpenses = new Dictionary<string, double>();

            Console.WriteLine("Enter Your usename and usernames of your friends and the amount you & they have to pay (one name and amount per line):");

            for (int i = 0; i < numberOfFriends; i++)
            {
                string inputLine = Console.ReadLine();
                string[] inputParts = inputLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (inputParts.Length != 2 || !double.TryParse(inputParts[1], out double paidAmount))
                {
                    Console.WriteLine($"Invalid input: '{inputLine}'. Expense not added.");
                    return;
                }

                string friendName = inputParts[0];

                if (!IsUserExists(friendName))
                {
                    Console.WriteLine($"User '{friendName}' does not exist. Expense not added.");
                    return;
                }

                friendNames.Add(friendName);
                friendExpenses[friendName] = paidAmount;
            }

            if (friendExpenses.Count == 0)
            {
                Console.WriteLine("No friend and expense data provided. Expense not added.");
                return;
            }

            friendNames.Add(currentUser);

            double totalPaidAmount = friendExpenses.Values.Sum();

            if (totalPaidAmount != totalAmount)
            {
                Console.WriteLine("Total amount paid by friends doesn't match the total expense amount. Expense not added.");
                return;
            }

            Console.Write("Enter the expense type: ");
            string expenseType = Console.ReadLine();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                Dictionary<string, double> friendNetExpenses = CalculateNetExpenses(connection, friendNames);

                SaveExpense(friendExpenses, friendNetExpenses, expenseType);

                Console.WriteLine("Expense added successfully!");
            }
        }





        public void ViewExpenses()
        {
            if (currentUser == "")
            {
                Console.WriteLine("You must log in first to view your expenses.");
                return;
            }

            Console.WriteLine("Your Expenses:");
            Console.WriteLine("+----+------------------+----------------+----------------+----------------+");

            try
            {
                List<ExpenseDetails> expenses = GetExpenseDetailsForUser(currentUser);

                Console.WriteLine($"|{"Id",-3} | {"Name",-16} | {"Amount",-14} | {"Date",-14} | {"Expense Type",-14} |");
                Console.WriteLine("+-----------------------+----------------+----------------+----------------+");

                foreach (var expense in expenses)
                {
                    Console.WriteLine($"|{expense.Id,-4}| {expense.FriendName,-16} |Rs {expense.Amount,-12} | {expense.Date,-14:yyyy-MM-dd} | {expense.ExpenseType,-14} |");
                }

                Console.WriteLine("+-----------------------+----------------+----------------+----------------+");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public void UpdateExpense()
        {
            if (currentUser == "")
            {
                Console.WriteLine("You must log in first to update an expense.");
                return;
            }

            ViewExpenses();

            Console.Write("Enter the ID of the expense you want to update: ");
            if (!int.TryParse(Console.ReadLine(), out int expenseId))
            {
                Console.WriteLine("Invalid input. Please enter a valid numeric ID.");
                return;
            }

            Console.Write("Enter the new amount for the expense: ");
            if (!double.TryParse(Console.ReadLine(), out double newAmount) || newAmount < 0)
            {
                Console.WriteLine("Invalid input. Please enter a valid positive numeric amount.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    ExpenseDetails existingExpense = GetExpenseDetailsById(connection, expenseId);

                    if (existingExpense == null)
                    {
                        Console.WriteLine("Expense with the provided ID not found.");
                        return;
                    }

                    double netChange = newAmount - existingExpense.Amount;

                    UpdateExpenseAmount(connection, expenseId, newAmount);

                    UpdateBalance(connection, existingExpense.FriendName, -netChange);
                    UpdateBalance(connection, currentUser, netChange);

                    Console.WriteLine("Expense updated successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        public void DeleteExpense()
        {
            if (currentUser == "")
            {
                Console.WriteLine("You must log in first to delete an expense.");
                return;
            }

            ViewExpenses();

            Console.Write("Enter the ID of the expense you want to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int expenseId))
            {
                Console.WriteLine("Invalid input. Please enter a valid numeric ID.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();


                    ExpenseDetails existingExpense = GetExpenseDetailsById(connection, expenseId);

                    if (existingExpense == null)
                    {
                        Console.WriteLine("Expense with the provided ID not found.");
                        return;
                    }

                    //USED FOR NET BALANCE CALCULATION
                    double netChange = -existingExpense.Amount;


                    DeleteExpenseById(connection, expenseId);


                    UpdateBalance(connection, existingExpense.FriendName, netChange);
                    UpdateBalance(connection, currentUser, -netChange);

                    Console.WriteLine("Expense deleted successfully!");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        private bool IsUserExists(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count > 0;
                }
            }
        }

        private void SaveExpense(Dictionary<string, double> friendExpenses, Dictionary<string, double> friendNetExpenses, string expenseType)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var friendName in friendExpenses.Keys)
                        {
                            double paidAmount = friendExpenses[friendName];


                            double newBalance = friendNetExpenses[friendName] - paidAmount;


                            UpdateBalance(connection, friendName, newBalance);


                            using (MySqlCommand command = new MySqlCommand("INSERT INTO Expenses (FriendName, Amount, Date, ExpenseType) VALUES (@FriendName, @Amount, @Date, @ExpenseType)", connection))
                            {
                                command.Parameters.AddWithValue("@FriendName", friendName);
                                command.Parameters.AddWithValue("@Amount", paidAmount);
                                command.Parameters.AddWithValue("@Date", DateTime.Now);
                                command.Parameters.AddWithValue("@ExpenseType", expenseType);
                                command.ExecuteNonQuery();
                            }
                        }


                        double currentUserBalance = GetBalanceForUser(currentUser);
                        double newCurrentUserBalance = currentUserBalance + friendExpenses[currentUser];
                        UpdateBalance(connection, currentUser, newCurrentUserBalance);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"You cannot add other users without adding yourself");
                    }
                }
            }
        }

        //USED FOR NET BALANCE CALCULATION
        private Dictionary<string, double> CalculateNetExpenses(MySqlConnection connection, List<string> friendNames)
        {
            Dictionary<string, double> friendNetExpenses = new Dictionary<string, double>();

            foreach (var friendName in friendNames)
            {
                friendNetExpenses[friendName] = GetBalanceForUser(friendName);
            }

            using (MySqlCommand command = new MySqlCommand("SELECT FriendName, SUM(Amount) AS TotalAmount FROM Expenses WHERE FriendName IN (@FriendNames) GROUP BY FriendName", connection))
            {
                command.Parameters.AddWithValue("@FriendNames", string.Join(",", friendNames));
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string friendName = reader.GetString("FriendName");
                        double totalAmount = reader.GetDouble("TotalAmount");

                        friendNetExpenses[friendName] -= totalAmount;
                    }
                }
            }

            return friendNetExpenses;
        }

        //USED FOR NET BALANCE CALCULATION
        private double GetBalanceForUser(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT Amount FROM Balances WHERE Username = @Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    object result = command.ExecuteScalar();

                    return result == null ? 0 : Convert.ToDouble(result);
                }
            }
        }

        //USED FOR NET BALANCE CALCULATION
        private void UpdateBalance(MySqlConnection connection, string username, double amount)
        {
            using (MySqlCommand selectCommand = new MySqlCommand("SELECT Amount FROM Balances WHERE Username = @Username", connection))
            {
                selectCommand.Parameters.AddWithValue("@Username", username);
                object result = selectCommand.ExecuteScalar();

                if (result != null && double.TryParse(result.ToString(), out double currentBalance))
                {
                    double newBalance = currentBalance + amount;

                    using (MySqlCommand updateCommand = new MySqlCommand("UPDATE Balances SET Amount = @Amount WHERE Username = @Username", connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Amount", newBalance);
                        updateCommand.Parameters.AddWithValue("@Username", username);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                else
                {

                    using (MySqlCommand insertCommand = new MySqlCommand("INSERT INTO Balances (Username, Amount) VALUES (@Username, @Amount)", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Username", username);
                        insertCommand.Parameters.AddWithValue("@Amount", amount);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private ExpenseDetails GetExpenseDetailsById(MySqlConnection connection, int expenseId)
        {
            using (MySqlCommand command = new MySqlCommand("SELECT Id,FriendName, Amount, Date, ExpenseType FROM Expenses WHERE Id = @ExpenseId", connection))
            {
                command.Parameters.AddWithValue("@ExpenseId", expenseId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ExpenseDetails expense = new ExpenseDetails
                        {
                            Id = reader.GetInt32("Id"),
                            FriendName = reader.GetString("FriendName"),
                            Amount = reader.GetDouble("Amount"),
                            Date = reader.GetDateTime("Date"),
                            ExpenseType = reader.GetString("ExpenseType")
                        };

                        return expense;
                    }
                }
            }

            return null;
        }

        private void UpdateExpenseAmount(MySqlConnection connection, int expenseId, double newAmount)
        {
            using (MySqlCommand command = new MySqlCommand("UPDATE Expenses SET Amount = @NewAmount WHERE Id = @ExpenseId", connection))
            {
                command.Parameters.AddWithValue("@NewAmount", newAmount);
                command.Parameters.AddWithValue("@ExpenseId", expenseId);
                command.ExecuteNonQuery();
            }
        }


        private double GetValidAmount()
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (double.TryParse(input, out double amount) && amount >= 0)
                {
                    return amount;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid positive amount.");
                }
            }

        }

        private int GetValidChoice(int minValue, int maxValue)
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (int.TryParse(input, out int choice) && choice >= minValue && choice <= maxValue)
                {
                    return choice;
                }
                else
                {
                    Console.WriteLine($"Invalid input. Please enter a valid choice between {minValue} and {maxValue}.");
                }
            }
        }

        private List<ExpenseDetails> GetExpenseDetailsForUser(string username)
        {
            List<ExpenseDetails> expenses = new List<ExpenseDetails>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT Id, FriendName, Amount, Date, ExpenseType FROM Expenses WHERE FriendName = @Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ExpenseDetails expense = new ExpenseDetails
                            {
                                Id = reader.GetInt32("Id"),
                                FriendName = reader.GetString("FriendName"),
                                Amount = reader.GetDouble("Amount"),
                                Date = reader.GetDateTime("Date"),
                                ExpenseType = reader.GetString("ExpenseType")
                            };

                            expenses.Add(expense);
                        }
                    }
                }
            }

            return expenses;
        }


        private void ViewBalance()
        {
            if (currentUser == "")
            {
                Console.WriteLine("You must log in first to view your balance.");
                return;
            }

            Console.WriteLine("Your Balance:");
            Console.WriteLine("+------------------+----------------+----------------+");

            try
            {

                double balance = GetBalanceForUser(currentUser);


                Console.WriteLine($"| Current User     | {currentUser,-13} | {balance,13:C} |");
                Console.WriteLine("+------------------+----------------+----------------+");


                List<string> sharedWith = GetFriendsForExpense();


                Console.WriteLine("| Shared with      | Expense        | Friend Balance |");
                Console.WriteLine("+------------------+----------------+----------------+");

                foreach (var friend in sharedWith)
                {
                    double friendBalance = GetBalanceForUser(friend);
                    double totalExpense = GetTotalExpenseForFriend(friend);

                    Console.WriteLine($"| {friend,-15} | {(-totalExpense),13:C} | {friendBalance,13:C} |");
                }

                Console.WriteLine("+------------------+----------------+----------------+");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private List<string> GetFriendsForExpense()
        {
            List<string> friends = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT DISTINCT FriendName FROM Expenses WHERE FriendName != @Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", currentUser);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            friends.Add(reader.GetString("FriendName"));
                        }
                    }
                }
            }

            return friends;
        }


        private double GetTotalExpenseForFriend(string friendName)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand("SELECT SUM(Amount) FROM Expenses WHERE FriendName = @FriendName", connection))
                {
                    command.Parameters.AddWithValue("@FriendName", friendName);
                    object result = command.ExecuteScalar();

                    return result == null || result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }
            }
        }


        private void DeleteExpenseById(MySqlConnection connection, int expenseId)
        {
            using (MySqlCommand command = new MySqlCommand("DELETE FROM Expenses WHERE Id = @ExpenseId", connection))
            {
                command.Parameters.AddWithValue("@ExpenseId", expenseId);
                command.ExecuteNonQuery();
            }
        }

    }


}



