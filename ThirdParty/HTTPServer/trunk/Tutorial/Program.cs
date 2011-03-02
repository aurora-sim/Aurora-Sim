using System;
using System.Collections.Generic;
using System.Text;

namespace Tutorial
{
    /// <summary>
    /// Automagicly loads all tutorials and presents them in a menu
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            List<Tutorial> _tutorials = new List<Tutorial>();
            int index = 1;
            while (true)
            {
                Type type = Type.GetType("Tutorial.Tutorial" + index + ".Tutorial" + index);
                if (type != null)
                    _tutorials.Add((Tutorial)Activator.CreateInstance(type));
                else
                    break;

                ++index;
            }

            Tutorial lastTutor = null;
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Tutorials");
                Console.WriteLine();
                for (int i = 0; i < _tutorials.Count; ++i)
                    Console.WriteLine((i+1).ToString().PadLeft(2) + " " + _tutorials[i].Name);

                Console.WriteLine();
                Console.Write("Select a tutorial and press enter, or type 'quit': ");
                string choice = Console.ReadLine();
                if (choice == "quit")
                    break;
                if (!int.TryParse(choice, out index))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Enter a NUMBER!");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    continue;
                }
                if (index < 0 || index > _tutorials.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("How about a number between 1 and " + (_tutorials.Count) + "??");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    continue;
                }

                Console.WriteLine();

                if (lastTutor != null)
                    lastTutor.EndTutorial();

                lastTutor = _tutorials[index - 1];
                lastTutor.StartTutorial();
            }
        }
    }
}
