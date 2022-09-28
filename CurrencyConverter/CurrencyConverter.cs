// Author: Jacob Gifford - jacobgifford22@gmail.com
// Description: Simple currency converter

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CurrencyConverter
{
    class Program
    {
        private List<Currency> _currencies;
        private MySqlConnection _conn;
        
        public class Currency
        {
            // Currency attributes
            [Key]
            [Required]
            public int CurrencyID { get; set; }
            [Required]
            public string CurrencyCode { get; set; }
            [Required]
            public double ExchangeRate { get; set; }
        }

        // Main function starts here
        public static void Main(string[] args)
        {
            Program program = new Program();
            
            // Initializes database connection and currency repository
            program.InitConnection();
            program.GetCurrencies();

            // Menu options
            Console.WriteLine("Currency Converter - by Jacob Gifford");
            string selection = "";

            // Repeats menu options until user quits
            while (selection != "q")
            {
                Console.WriteLine("\n--------------------------------");
                Console.WriteLine("[x] - Exchange currency");
                Console.WriteLine("[c] - Create new currency entry");
                Console.WriteLine("[r] - Read currency database");
                Console.WriteLine("[u] - Update currency entry");
                Console.WriteLine("[d] - Delete currency entry");
                Console.WriteLine("--------------------------------\n");
                Console.WriteLine("Choose a menu option to continue, or press [q] to quit.");
                selection = Convert.ToString(Console.ReadKey().KeyChar);
                Console.WriteLine("");

                // Checks which option was selected
                if (selection == "x")
                {
                    program.ExchangeMenuOption(program);
                }
                else if (selection == "c")
                {
                    program.CreateMenuOption(program);
                }
                else if (selection == "r")
                {
                    program.ReadMenuOption();
                }
                else if (selection == "u")
                {
                    Console.WriteLine("Which currency would you like to update?");
                    var currencyCode = program.ValidateCurrencyInput();
                    Currency c = program.GetCurrency(currencyCode);

                    program.UpdateMenuOption(program, c);
                }
                else if (selection == "d")
                {
                    Console.WriteLine("Which currency would you like to delete?");
                    var currencyCode = program.ValidateCurrencyInput();
                    Currency c = program.GetCurrency(currencyCode);

                    program.DeleteMenuOption(c);
                }
            }

            // Close database connection
            program.CloseConnection();
        }

        // Converts currency from one type to another
        public decimal ConvertCurrency(string currencyFrom, string currencyTo, decimal amount)
        {
            Currency exchangeFrom = _currencies.Single(x => x.CurrencyCode == currencyFrom);
            Currency exchangeTo = _currencies.Single(x => x.CurrencyCode == currencyTo);

            decimal convertedAmount = (Convert.ToDecimal(exchangeTo.ExchangeRate) * amount) / Convert.ToDecimal(exchangeFrom.ExchangeRate);

            return convertedAmount;
        }

        // Initializes database connection
        private void InitConnection()
        {
            string connStr = "server=localhost;user=root;database=currencyconverter;port=3306;password=admin";
            
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            _conn = conn;
        }

        // Closes database connection
        private void CloseConnection()
        {
            _conn.Close();
        }

        // Gets a list of Currency objects from the database
        private void GetCurrencies()
        {
            List<Currency> currencies = new List<Currency>();

            // SQL query selects all rows from currency table
            string sql = "select * from currencies;";
            MySqlCommand cmd = new MySqlCommand(sql, _conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            // Reads database into list of Currency objects
            while (rdr.Read())
            {
                Currency currentCurrency = new Currency
                {
                    CurrencyID = (int)rdr.GetValue(0),
                    CurrencyCode = (string)rdr.GetValue(1),
                    ExchangeRate = (double)rdr.GetValue(2)
                };

                currencies.Add(currentCurrency);
            }
            rdr.Close();

            _currencies = currencies;
        }

        // Checks to see if currency code input matches a currency code in the database
        private string ValidateCurrencyInput()
        { 
            IEnumerable<string> validCurrencyCodes = _currencies.Select(x => x.CurrencyCode);

            Console.WriteLine("Enter currency code (ex: USD): ");
            string currencyCode = Console.ReadLine().ToUpper();

            // Prompts user to input a valid currency code if entry is not in list
            while (!validCurrencyCodes.Contains(currencyCode))
            {
                Console.WriteLine("Please enter a valid currency code (ex: USD): ");
                currencyCode = Console.ReadLine().ToUpper();
            }

            return currencyCode;
        }

        // Returns a currency object given a currency code string
        private Currency GetCurrency(string currencyCode)
        {
            Currency currency = _currencies.Single(x => x.CurrencyCode == currencyCode);
            
            return currency;
        }

        // Calls the ConvertCurrency function and outputs conversion results
        private void ExchangeMenuOption(Program program)
        {
            // Validates inputted currency codes
            Console.WriteLine("\nWhich currency are you converting from?");
            var currencyCodeFrom = program.ValidateCurrencyInput();
            Console.WriteLine("Which currency are you converting to?");
            var currencyCodeTo = program.ValidateCurrencyInput();

            // Checks if amount entered is numeric
            Console.WriteLine("Enter amount of currency to convert (ex: 1.0): ");
            var isNumeric = decimal.TryParse(Console.ReadLine(), out decimal amount);
            while (!isNumeric)
            {
                Console.WriteLine("Enter a valid number for the amount of currency to convert (ex: 1.0): ");
                isNumeric = decimal.TryParse(Console.ReadLine(), out amount);
            }

            // Converts currency
            decimal convertedAmount = program.ConvertCurrency(currencyCodeFrom, currencyCodeTo, amount);

            // Displays conversion results
            Console.WriteLine("\n--------------------------------");
            Console.WriteLine(amount + " " + currencyCodeFrom + " equals " + Math.Round(convertedAmount, 4) + " " + currencyCodeTo + ".");
            Console.WriteLine("--------------------------------");
        }

        // Creates a new currency record in the database
        private void CreateMenuOption(Program program)
        {
            IEnumerable<string> validCurrencyCodes = _currencies.Select(x => x.CurrencyCode);

            // Checks if currency code entered is unique and has a length of 3
            Console.WriteLine("\nEnter a unique currency code (ex: USD): ");
            string currencyCode = Console.ReadLine().ToUpper();
            while (currencyCode.Length != 3 || validCurrencyCodes.Contains(currencyCode))
            {
                Console.WriteLine("Please enter a valid, unique, three-letter currency code (ex: USD): ");
                currencyCode = Console.ReadLine().ToUpper();
            }

            // Checks if exchange rate entered is numeric
            Console.WriteLine("Enter exchange rate (ex: 1.0): ");
            var isNumeric = double.TryParse(Console.ReadLine(), out double exchangeRate);
            while (!isNumeric)
            {
                Console.WriteLine("Please enter a valid number for the exchange rate (ex: 1.0): ");
                isNumeric = double.TryParse(Console.ReadLine(), out exchangeRate);
            }

            // Updates record in database
            string sql = "insert into currencies (CurrencyCode, ExchangeRate) values " 
                + "(\"" + currencyCode + "\", " + exchangeRate + ");";
            MySqlCommand cmd = new MySqlCommand(sql, _conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Close();

            // Refreshes currency list
            program.GetCurrencies();
            Currency c = program.GetCurrency(currencyCode);

            // Displays new currency
            Console.WriteLine("\nCurrencyID | CurrencyCode | ExchangeRate");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(c.CurrencyID + "\t   | " + c.CurrencyCode + "\t  | " + c.ExchangeRate);
            Console.WriteLine("----------------------------------------");
        }

        // Prints rows from the currency table
        private void ReadMenuOption()
        {
            Console.WriteLine("\nCurrencyID | CurrencyCode | ExchangeRate");
            Console.WriteLine("----------------------------------------");

            foreach (Currency c in _currencies)
            {
                Console.WriteLine(c.CurrencyID + "\t   | " + c.CurrencyCode + "\t  | " + c.ExchangeRate);
            }

            Console.WriteLine("----------------------------------------");
        }

        // Updates currency record in database
        private void UpdateMenuOption(Program program, Currency c)
        {
            IEnumerable<string> validCurrencyCodes = _currencies.Select(x => x.CurrencyCode);
            
            // Displays currency
            Console.WriteLine("\nCurrencyID | CurrencyCode | ExchangeRate");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(c.CurrencyID + "\t   | " + c.CurrencyCode + "\t  | " + c.ExchangeRate + "\n");
            Console.WriteLine("----------------------------------------");

            // Removes current currency code from list to allow it to be entered again
            List<string> validCodes = validCurrencyCodes.ToList();
            validCodes.Remove(c.CurrencyCode);

            // Checks if currency code entered is unique and has a length of 3
            Console.WriteLine("Enter a unique currency code (ex: USD): ");
            string currencyCode = Console.ReadLine().ToUpper();
            while (currencyCode.Length != 3 || validCodes.Contains(currencyCode))
            {
                Console.WriteLine("Please enter a valid, unique, three-letter currency code (ex: USD): ");
                currencyCode = Console.ReadLine().ToUpper();
            }

            // Checks if exchange rate entered is numeric
            Console.WriteLine("Enter updated exchange rate (ex: 1.0): ");
            var isNumeric = double.TryParse(Console.ReadLine(), out double exchangeRate);
            while (!isNumeric)
            {
                Console.WriteLine("Please enter a valid number for the exchange rate (ex: 1.0): ");
                isNumeric = double.TryParse(Console.ReadLine(), out exchangeRate);
            }

            // Updates record in database
            string sql = "update currencies set CurrencyCode = \"" + currencyCode 
                + "\", ExchangeRate = " + exchangeRate 
                + " where CurrencyID = " + c.CurrencyID + ";";
            MySqlCommand cmd = new MySqlCommand(sql, _conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Close();

            // Refreshes currency list
            program.GetCurrencies();
            c = program.GetCurrency(currencyCode);

            // Displays updated currency
            Console.WriteLine("\nCurrencyID | CurrencyCode | ExchangeRate");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(c.CurrencyID + "\t   | " + c.CurrencyCode + "\t  | " + c.ExchangeRate);
            Console.WriteLine("----------------------------------------");
        }

        // Removes a currency record from the database
        private void DeleteMenuOption(Currency c)
        {
            Console.WriteLine("\nAre you sure you want to delete the entry for " + c.CurrencyCode + "?");
            Console.WriteLine("Press [y] or [n]: ");
            string selection = Convert.ToString(Console.ReadKey().KeyChar);
            
            if (selection == "y")
            {
                // Deletes record from database
                string sql = "delete from currencies where CurrencyID = " + c.CurrencyID + ";";
                MySqlCommand cmd = new MySqlCommand(sql, _conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                rdr.Close();

                // Removes object from currency list
                _currencies.Remove(c);
            }    
        }
    }
}