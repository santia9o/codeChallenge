using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Challenge_Santiago
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string[] csvArr;
        DataTable dt;
        List<Locations> parsedLocations = new List<Locations>();
        List<Locations> nearestLocations = new List<Locations>();
        bool successParse = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please select the Data.CSV file to begin");
            OpenFileDialog ofd = new OpenFileDialog();

            dt = new DataTable();
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("City", typeof(string));
            dt.Columns.Add("State", typeof(string));
            dt.Columns.Add("Zip", typeof(int));
            dt.Columns.Add("Latitude", typeof(double));
            dt.Columns.Add("Longitude", typeof(double));

            do
            {
                try
                {
                    ParseCSVFile(ofd);
                }

                catch (Exception)
                {
                    if (MessageBox.Show("The file selected could not be parsed successfully. Would you like to try again? Please be sure to use the Data.csv file provided and close any other open instances.",
                    "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        successParse = false;
                    }
                    else
                    {
                        successParse = true;
                        App.Current.Shutdown();  //Close the app
                    }
                }
            }
            while (successParse == false);
        }

        private void ParseCSVFile(OpenFileDialog f)
        {
            f.ShowDialog();
            using (StreamReader sr = new StreamReader(f.FileName))
            {
                string headerline = sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    csvArr = sr.ReadLine().Split(',');
                    Locations addresses = new Locations();
                    addresses.Address = csvArr[0];
                    addresses.City = csvArr[1];
                    addresses.State = csvArr[2];
                    addresses.Zip = Convert.ToInt32(csvArr[3]);
                    addresses.Latitude = Convert.ToDouble(csvArr[4]);
                    addresses.Longitude = Convert.ToDouble(csvArr[5]);
                    parsedLocations.Add(addresses);

                    dt.Rows.Add(csvArr);
                }

                DataView dv = new DataView(dt);
                dtGridView.ItemsSource = dv; //Show the addresses from csv in the datagrid
            }
            successParse = true;

            if (dtGridView.Items.Count > 0)
            {
                dbAddressLabel.Foreground = Brushes.Black;
            }
            else
            {
                dbAddressLabel.Foreground = Brushes.LightGray;
            }
        }

        private void SearchAddressTxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataView dv = dt.DefaultView;
            string filter = searchAddressTxtBox.Text;
            if (string.IsNullOrEmpty(filter))
            {
                dv.RowFilter = null;
            }
            else
            {
                dv.RowFilter = string.Format("Address Like '%{0}%'", filter); //Only search in address column for text entered
                dtGridView.ItemsSource = dv;
            }

            if (dtGridView.Items.Count > 0)
            {
                dbAddressLabel.Foreground = Brushes.Black;
            }
            else
            {
                dbAddressLabel.Foreground = Brushes.LightGray;
            }
        }

        private void MyDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            nearestLocations.Clear();

            if (dtGridView.SelectedItem == null)
            {
                MessageBox.Show("There are no addresses selected");
            }
            else
            {
                DataRowView dr = dtGridView.SelectedItem as DataRowView;
                DataRow dr1 = dr.Row;

                string selectedAddress = dr1.ItemArray[0].ToString();
                double latCord = Convert.ToDouble(dr1.ItemArray[4]);
                double longCord = Convert.ToDouble(dr1.ItemArray[5]);
                currentAddressLabel.Content = string.Format("These are the addresses near {0}  - Latitude: {1} - Longitude: {2} ", selectedAddress, latCord, longCord);
                foreach (var item in parsedLocations)
                {
                    DoMath(latCord, longCord, item);
                }

                List<Locations> sortedLocations = nearestLocations.OrderBy(p => p.Miles).ToList(); //Sort all the addresses after doing math

                dtGridView2.ItemsSource = ShowNearest(sortedLocations); //Display in second datagrid
            }
        }

        private void DoMath(double selectedA, double selectedB, Locations item)
        {
            double lat1 = selectedA;
            double lon1 = selectedB;
            double lat2 = item.Latitude;
            double lon2 = item.Longitude;
            double distanceInMiles = Haversine(lat1, lon1, lat2, lon2);

            Locations te = new Locations();
            te.Miles = distanceInMiles;
            te.Address = item.Address;
            te.City = item.City;
            te.State = item.State;
            te.Zip = item.Zip;
            te.Latitude = item.Latitude;
            te.Longitude = item.Longitude;

            if (distanceInMiles != 0)  //If the distance is 0, it is a duplicate of the current address selected and therefore we don't add it to our "nearest" list.
            {
                nearestLocations.Add(te);
            }
        }

        double Radians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            //Formula for computing distances between two points on the surface of a sphere using the latitude and longitude of two locations.
            double radius = 6371;
            double phi2 = Radians(lat2);
            double phi1 = Radians(lat1);
            double delta_phi = phi2 - phi1;

            double lamda2 = Radians(lon2);
            double lamda1 = Radians(lon1);
            double deltha_lambda = lamda2 - lamda1;

            double inner_p = Math.Sqrt(Math.Pow(Math.Sin(delta_phi / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Pow(Math.Sin(deltha_lambda / 2), 2));

            double distance = 2 * radius * Math.Asin(Math.Sin(inner_p)) / 1.6;
            return Math.Round(distance, 3);
        }

        private List<Locations> ShowNearest(List<Locations> p)
        {
            List<Locations> nearby = new List<Locations>();

            for (int i = 0; i < 10; i++)
            {
                nearby.Add(new Locations()
                {
                    Miles = p[i].Miles,
                    Address = p[i].Address,
                    City = p[i].City,
                    State = p[i].State,
                    Zip = p[i].Zip,
                    Latitude = p[i].Latitude,
                    Longitude = p[i].Longitude,
                });
            }
            return nearby;
        }

        private void SearchAddressTxtBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //User wants to filter for a new address, reset label and second data grid
            currentAddressLabel.Content = null;
            dtGridView2.ItemsSource = null;
            dtGridView2.Items.Refresh();
        }
    }
}
