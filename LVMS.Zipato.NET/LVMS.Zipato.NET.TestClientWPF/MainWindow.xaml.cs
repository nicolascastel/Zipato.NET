using System;
using System.Collections.Generic;
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
using System.Net;
using LVMS.Zipato.NET.TestClientWPF.Properties;
using System.IO;
namespace LVMS.Zipato.NET.TestClientWPF
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ZipatoClient client = new ZipatoClient();
        IEnumerable<Model.Endpoint> onOffEndpoints;
        public MainWindow()
        {
            InitializeComponent();


        }
        private async void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var credentials = GetCredentials();
            if (credentials == null)
            {
                this.credentialPromptGroupBox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {

                try
                {
                    await Connect(credentials);
                
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR " + ex.Message, "ERROR", MessageBoxButton.OK);
                }

            }
        }

        private async Task Connect(System.Net.NetworkCredential pCredentials )
        {
             try
                {
            await client.LoginAsync(pCredentials.UserName, pCredentials.Password);
            this.credentialPromptGroupBox.Visibility = System.Windows.Visibility.Hidden;
            this.infoTextBlock.Text = "logged : " + pCredentials.UserName;
                }
             catch (Exception ex)
             {
                 MessageBox.Show("ERROR " + ex.Message, "ERROR", MessageBoxButton.OK);
             }
        }

        private async void goButton_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                var credentials = new NetworkCredential(this.credentialLogin.Text, this.credentialPassword.Password);
                Task t = Connect(credentials);
                await t;
                this.infoTextBlock.Text = "logged";
                //   onContinue = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR " + ex.Message, "ERROR", MessageBoxButton.OK);
            }

        }

        //private async void allumerButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (String.IsNullOrEmpty(this.guidLumiere.Text))
        //    {
        //        return;
        //    }

        //    Task a = allumer();
        //    try
        //    {

        //        await a;
        //        //   onContinue = true;
        //    }
        //    catch (Exception ex)
        //    {

        //        MessageBox.Show("Une erreur est survenue lors de l'appel à R. " + ex.Message, "Erreur", MessageBoxButton.OK);

        //    }
        //}


        private async void GetEndpointsWithOnOffAsyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool includeValues = OnOffIncludeValuesCheckBox.IsChecked.Value;
                bool hideZipaboxInternalEndpoints = OnOffhideZipaboxInternalEndpointsCheckBox.IsChecked.Value;
                bool hideHidden = OnOffhideHiddenCheckBox.IsChecked.Value;
                bool allowCache = OnOffallowCacheCheckBox.IsChecked.Value;

                 onOffEndpoints = await client.GetEndpointsWithOnOffAsync(includeValues, hideZipaboxInternalEndpoints, hideHidden, allowCache);
                dgEndPoints.ItemsSource = onOffEndpoints.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error " + ex.Message);
            }
        }

        private async void GetAllEndpointsWithOnOffAsyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool allowCache = allEndPointallowCacheCheckBox.IsChecked.Value;
                Enums.EndpointGetModes mode = Enums.EndpointGetModes.IncludeEndpointInfoOnly;

                switch (((ComboBoxItem)allEndpointGetModesComboBox.SelectedItem).Content.ToString())
                {
                    case "None":
                        mode = Enums.EndpointGetModes.None;
                        break;
                    case "IncludeEndpointInfoOnly":
                        mode = Enums.EndpointGetModes.IncludeEndpointInfoOnly;
                        break;
                    case "IncludeFullAttributes":
                        mode = Enums.EndpointGetModes.IncludeFullAttributes;
                        break;
                    case "IncludeFullAttributesWithValues":
                        mode = Enums.EndpointGetModes.IncludeFullAttributesWithValues;
                        break;
                }

                onOffEndpoints = await client.GetEndpointsAsync(mode, allowCache);
                dgEndPoints.ItemsSource = onOffEndpoints.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error " + ex.Message);
            }
        }


        private NetworkCredential GetCredentials()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Settings.Default.CredentialsFile) && File.Exists(
                    Environment.ExpandEnvironmentVariables(Settings.Default.CredentialsFile)))
                {
                    var lines = File.ReadLines(Environment.ExpandEnvironmentVariables(Settings.Default.CredentialsFile)).ToArray();


                    return new NetworkCredential(lines[0], lines[1]);
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't read credentials from file. Error: " + ex.Message);
                return null;
            }

        }



    }
}
