using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive
{
    class DB : IDisposable
    {
        public SQLiteConnection SQLite;
        private sealed class SingletonCreator
        {
            private static readonly DB instance = new DB();
            public static DB Instance { get { return instance; } }
        }

        public static DB Instance
        {
            get { 
                return SingletonCreator.Instance; 
            }
        }

        public void Connect()
        {
            if (SQLite != null)
            {
                SQLite.Close();
            }
            SQLite = new SQLiteConnection("Data Source=:memory:;New=True;");
            SQLite.Open();


            Execute(@"CREATE TABLE file_parts( 
                uid INTEGER NOT NULL, 
                fid INTEGER NOT NULL, 
                block_id INTEGER NOT NULL,
                byte INTEGER NOT NULL,
                byte_offset INTEGER NOT NULL,
                status TINYINT NOT NULL, 
                    PRIMARY KEY ( uid, fid, block_id))");
            Execute(@"INSERT OR IGNORE INTO file_parts 
                        ( uid, fid, block_id, byte, byte_offset, status ) VALUES 
                        ( 0,    0,     0,      0,      0,          2)");


        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public SQLiteDataReader Execute(string textCommand)
        {
            SQLiteCommand command = new SQLiteCommand(textCommand, SQLite);
            //Console.WriteLine(textCommand);
            //command.CommandText = textCommand;
            //command.CommandType = CommandType.Text;
            
            return command.ExecuteReader();
        }

        public void Dispose()
        {
            if (SQLite != null)
            {
                SQLite.Close();
            }
        }
    }
}
