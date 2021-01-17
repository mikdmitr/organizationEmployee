using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace organizationEmployee
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string connectionString;
        
        public DataSet ds;

        SqlDataAdapter adapter ;


        public MainWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            ds = new DataSet("OrganizationAndEmployee");
 
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Очищаем Dataset
                ds.Clear();

                SelectDataFillDataSet(connection, "Organization");
                SelectDataFillDataSet(connection, "Employee");

            }
        }


        private void SelectDataFillDataSet(SqlConnection connection,String Table)
        {
            // Создаем объект DataAdapter
            adapter = new SqlDataAdapter("SELECT * FROM " +Table, connection);
            // Заполняем Dataset
            adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            adapter.Fill(ds,Table);
            if (Table == "Employee")
            {
                ds.Tables["Employee"].Columns["OrgId"].DefaultValue = 0;
                //ds.Tables["Employee"].TableNewRow += mainWindow_TableNewRow;
            }
        }

        private void mainWindow_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
            MessageBox.Show("Проверка TableNewRow");
        }

        private void RefreshDataGrid()
        {
            // Отображаем данные датасет в гриде
            dgOrganiztion.ItemsSource = ds.Tables["Organization"].DefaultView;
            dgOrganiztion.IsReadOnly = false;
            dgOrganiztion.Columns.Where(a => a.Header.ToString() == "Id").FirstOrDefault().IsReadOnly = true;

            dgOrganiztion.SelectedCellsChanged += dgOrganiztion_SelectedCellsChanged;

           
            dgEmployee.ItemsSource = ds.Tables["Employee"].DefaultView;
            dgEmployee.IsReadOnly = false;
            dgEmployee.Columns.Where(a => a.Header.ToString() == "Id").FirstOrDefault().IsReadOnly = true;
                       
        }



        private void dgOrganiztion_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try 
            {
                ds.Tables["Employee"].DefaultView.RowFilter = (DataRowView)dgOrganiztion.SelectedItem != null ? "OrgId=" + ((DataRowView)dgOrganiztion.SelectedItem).Row.ItemArray[0].ToString() : "";
                ds.Tables["Employee"].Columns[1].DefaultValue = (DataRowView)dgOrganiztion.SelectedItem != null ? Int32.Parse(((DataRowView)dgOrganiztion.SelectedItem).Row.ItemArray[0].ToString()) : 0;
            }
            catch { }
            
        }

        private void buttonLoadDataFromDB_Click(object sender, RoutedEventArgs e)
        {            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Очищаем Dataset
                ds.Clear();

                // Заполняем Dataset
                SelectDataFillDataSet(connection, "Organization");
                SelectDataFillDataSet(connection, "Employee");
                
            }
            // Отображаем данные
            RefreshDataGrid();

            
        }

        private void setTableMapping()
        {
            foreach(DataTable table in ds.Tables)
            {
                adapter.TableMappings.Add(table.TableName, table.TableName);
                foreach (DataColumn column in table.Columns) 
                {
                    adapter.TableMappings[table.TableName].ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
            }
        }

        private void UpdateDB(SqlConnection connection, String Table)
        {
            // Создаем объект DataAdapter
            adapter = new SqlDataAdapter("SELECT * FROM " + Table, connection);
            // Делаем Mapping
            setTableMapping();
            //передаем данные в базу
            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);
            try { adapter.Update(ds, Table); }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
            
        }

        private void buttonUpdateData_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                UpdateDB(connection, "Organization");
                UpdateDB(connection, "Employee");


                // Очищаем Dataset
                ds.Clear();

                // Заполняем Dataset
                SelectDataFillDataSet(connection, "Organization");
                SelectDataFillDataSet(connection, "Employee");

            }

            // Отображаем данные
            RefreshDataGrid();
        }

        private void buttonImportDataFromCSV_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "organizationListToImportExample"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "CSV-files (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;                

                if (System.IO.File.Exists(filename))
                {
                    string line;
                    int counter = 0;

                    System.IO.StreamReader file = new System.IO.StreamReader(filename, Encoding.GetEncoding(1251));

                    while ((line = file.ReadLine()) != null)
                    {                        
                        string[] words = line.Split(new char[] { ';' });

                        if ((words.Count()+1==ds.Tables["Organization"].Columns.Count) )
                        {
                            DataRow row;
                            row = ds.Tables["Organization"].NewRow();
                            for (int i = 1; i < ds.Tables["Organization"].Columns.Count; i++)
                                row[i] = words[i - 1];
                            ds.Tables["Organization"].Rows.Add(row);
                            counter++;
                        }        

                    }

                    file.Close();

                    MessageBox.Show("Добавлено "+ counter.ToString()+ "записей");

                }
            }
            // Отображаем данные
            RefreshDataGrid();
        }

        private void buttonExportDataToCSV_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "employeeExportedList"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "CSV-files (.csv)|*.csv"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;

                int counter = 0;

                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.GetEncoding(1251)))
                {
           
                    List<DataRow> dataToExport = new List<DataRow>();
           
                    int orgIdToExport= (DataRowView)dgOrganiztion.SelectedItem != null ? Int32.Parse(((DataRowView)dgOrganiztion.SelectedItem).Row.ItemArray[0].ToString()) : 0;

                    foreach (DataRow dataRow in ds.Tables["Employee"].Rows)
                    {
                        if (orgIdToExport > 0)
                        {
                            if(dataRow.ItemArray[1].ToString()== orgIdToExport.ToString())
                            {
                                dataToExport.Add(dataRow);
                            }
                        }
                        else
                            dataToExport.Add(dataRow);
                    }

                    

                    foreach (DataRow dataRow in dataToExport)
                    {
                        sw.WriteLine(dataRow.ItemArray.Select(a => a.ToString()).Aggregate((a, b) => a + ";" + b));
                        counter++;
                    }                  

                }
                MessageBox.Show("Экспортировано " + counter.ToString() + "записей");
            }
        }
    }
}
