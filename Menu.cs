using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class Menu
    {
        public void ShowMainMenu(List<string> agents, List<string> targets)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Main Menu ===");
                Console.WriteLine("1. Print aware agents");
                Console.WriteLine("2. Print dangerous targets");
                Console.WriteLine("0. Exit");
                Console.Write("Select an option: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        PrintList("Aware Agents", agents);
                        break;
                    case "2":
                        PrintList("Dangerous Targets", targets);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private void PrintList(string title, List<string> items)
        {
            Console.Clear();
            Console.WriteLine($"=== {title} ===");
            if (items == null || items.Count == 0)
            {
                Console.WriteLine("No items to display.");
            }
            else
            {
                foreach (var item in items)
                {
                    Console.WriteLine("- " + item);
                }
            }
            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }
    }
}
