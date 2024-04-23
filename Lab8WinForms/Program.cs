using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lab8WinForms;

namespace Lab8WinForms
{

    // Интерфейс (он же вид). Отображает данные из модели, передает команды пользователя презентеру
    interface IView 
    {
        string FirstDirectory();
        string SecondDirectory();

        string LogDirectory();

        void TrySynchronize(List<string> logMessage);

        event EventHandler<EventArgs> SyncronizeDirectoriesEvent;

        event EventHandler<EventArgs> Inversion;

        event EventHandler<EventArgs> SwitchLogDirectory;
    }
    

    // Модель. Основная часть работы программы происходит здесь
    class Model 
    {
        
        private bool isInverted = false;
        private XmlLogger newLog;
        private JsonLogger newJsonLog;

        

        // Для кнопки "инверсия", делающий главной директорию 2, и для возвращения к обычному виду работы программы при повторном нажатии
        public void Invert ()
        {
            newLog.CreateLog("Произведена инверсия");
            newJsonLog.CreateLog("Произведена инверсия");
            isInverted = !isInverted;
        }


        //Установка пути записей логов
        public void SetLogPaths(string path) 
        {
            newLog = new XmlLogger(path + "\\log.xml");
            newJsonLog = new JsonLogger(path + "log.json");
        }

        //Проверка для "рекомендации"
        public string CheckForRecomendation () 
        {
            string resultOfCheck = newLog.GetLastString();
            return resultOfCheck;
        }

        // Основной код по синхронизации директорий
        public List<string> SynchronizeDirectories(string firstDirectory, string secondDirectory)
        {
            DirectoryInfo mainDirectoryInfo = new DirectoryInfo(firstDirectory);
            DirectoryInfo targetDirectoryInfo  = new DirectoryInfo(secondDirectory);
            List<string> resultofSynchronize;

            //Существует ли директория?
            if (!mainDirectoryInfo.Exists || !targetDirectoryInfo.Exists)
            {
                newLog.CreateLog("Одна или 2 директории не найдены");
                newJsonLog.CreateLog("Одна или 2 директории не найдены");

                throw new DirectoryNotFoundException("Одна или 2 директории не найдены");
            }

            // Проверка наличия инверсии
            if (!isInverted)
            {
                resultofSynchronize = InnerSynchronizeDirectories(mainDirectoryInfo, targetDirectoryInfo);
                return resultofSynchronize;
            }
            else
            {
                resultofSynchronize = InnerSynchronizeDirectories(targetDirectoryInfo , mainDirectoryInfo);
                return resultofSynchronize;
            }
            
        }

        private List<string> InnerSynchronizeDirectories(DirectoryInfo mainDirectoryInfo, DirectoryInfo targetDirectoryInfo )
        {
            List<string> innerResultOfSynchronize = new List<string>();
            bool isNeedToSynchronize = false;

            // Поиск и (при необходимости) замена файлов из "целевой" директории файлами из "главной" директории
            foreach (FileInfo directoryFile in mainDirectoryInfo.GetFiles())
            {
                FileInfo targetFileInOtherDirectory = new FileInfo(Path.Combine(targetDirectoryInfo .FullName, directoryFile.Name));  // Combine есть объединение строк в одну

                if (!targetFileInOtherDirectory.Exists || targetFileInOtherDirectory.LastWriteTime != directoryFile.LastWriteTime) // Не существует или записан ранее - заменяем
                {
                    newLog.CreateLog($"Файл {directoryFile.Name} изменен");
                    newJsonLog.CreateLog($"Файл {directoryFile.Name} изменен");

                    //Можно ли скопировать файл?
                    try
                    {
                        File.Copy(directoryFile.FullName, targetFileInOtherDirectory.FullName, true);
                    }
                    catch (Exception fileException) 
                    {

                        newLog.CreateLog("Ошибка при копировании файла." + fileException.ToString());
                        newJsonLog.CreateLog("Ошибка при копировании файла." + fileException.ToString());

                        throw new Exception("Ошибка при копировании файла.", fileException);
                    }

                    innerResultOfSynchronize.Add($"Файл {directoryFile.Name} изменен");
                    isNeedToSynchronize = true;
                }
            }

            // Если в "главной" директории файла нет, удаляем его в "целевой"
            foreach (FileInfo directoryFile in targetDirectoryInfo.GetFiles())
            {
                FileInfo fileInMainDirectory = new FileInfo(Path.Combine(mainDirectoryInfo.FullName, directoryFile.Name));

                if (!fileInMainDirectory.Exists)
                {
                    newLog.CreateLog($"Файл {directoryFile.Name} удален");
                    newJsonLog.CreateLog($"Файл {directoryFile.Name} изменен");

                    //Можно ли удалить файл?
                    try
                    {
                        directoryFile.Delete();
                    }
                    catch (Exception fileException)
                    {
                        newLog.CreateLog("Ошибка при удалении файла." + fileException.ToString());
                        newJsonLog.CreateLog("Ошибка при удалении файла." + fileException.ToString());

                        throw new Exception("Ошибка при удалении файла.", fileException);
                    }

                    innerResultOfSynchronize.Add($"Файл {directoryFile.Name} удален");
                    isNeedToSynchronize = true;
                }
            }

            // Если не было попыток синхронизировать, сообщаем, что это и не требовалось.
            if (!isNeedToSynchronize) 
            {
                newLog.CreateLog("Не нужно синхронизировать");
                newJsonLog.CreateLog("Не нужно синхронизировать");

                innerResultOfSynchronize.Add("Не нужно синхронизировать");
                string checkResult = CheckForRecomendation();

                if (checkResult.Contains("Не нужно синхронизировать")) 
                {
                    innerResultOfSynchronize.Add("Лог показывает, что в прошлый раз ");
                    innerResultOfSynchronize.Add("директории были идентичны.");
                    innerResultOfSynchronize.Add("Рекомендуем временно ");
                    innerResultOfSynchronize.Add("приостановить попытки синхронизации.");
                }
            }

            return innerResultOfSynchronize;
        }
    }

    // Презентер. Извлекает данные из модели, передает в вид. Обрабатывает события
    class Presenter 
    {
        private IView mainView;
        private Model model;

        public Presenter(IView inputView) 
        {
            mainView = inputView;
            model = new Model();

            mainView.SyncronizeDirectoriesEvent += new EventHandler<EventArgs> (Synchronize);
            mainView.Inversion += new EventHandler<EventArgs> (Inversion);
            mainView.SwitchLogDirectory += new EventHandler<EventArgs> (SwitchLogDirectory);
        }

        // Обработка события "Инверсия"
        private void Inversion(object sender, EventArgs inputEvent)
        {
            model.Invert();
        }

        // Обработка события "Смена директории логов"
        private void SwitchLogDirectory(object sender, EventArgs inputEvent) 
        {
            model.SetLogPaths(mainView.LogDirectory());
        }

        // Обработка события "Синхронизация"
        private void Synchronize(object sender, EventArgs inputEvent) 
        {
            List<string> resultOfSynchronization = model.SynchronizeDirectories(mainView.FirstDirectory(),mainView.SecondDirectory());

            mainView.TrySynchronize(resultOfSynchronization);
        }
    }
    // Тут производится запуск
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
