using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Bluetooth;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Support.V4.App;
using TaskStackBuilder = Android.Support.V4.App.TaskStackBuilder;
using Android.Support.Graphics.Drawable;

namespace CarritoArduino
{
    [Activity(Label = "CarritoArduino", MainLauncher = true, Icon = "@mipmap/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : Activity
    {

        ImageButton BotonBlue, BotonU, BotonD, BotonR, BotonL, BotonS;
        bool Conectado = false;
        //Creamos las variables necesarios para trabajar
        //Widgets
        //String a enviar
        private Java.Lang.String dataToSend;
        //Variables para el manejo del bluetooth Adaptador y Socket
        private BluetoothAdapter mBluetoothAdapter = null;
        private BluetoothSocket btSocket = null;
        //Streams de lectura I/O
        private Stream outStream = null;
        private Stream inStream = null;
        //MAC Address del dispositivo Bluetooth
        private static string address = "20:16:04:08:16:11";
        //Id Unico de comunicacion
        private static UUID MY_UUID = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
        double Valores;
        int ValorFinal;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            BotonBlue = FindViewById<ImageButton>(Resource.Id.btnBP);
            BotonU = FindViewById<ImageButton>(Resource.Id.btnU);
            BotonD = FindViewById<ImageButton>(Resource.Id.btnD);
            BotonR = FindViewById<ImageButton>(Resource.Id.btnR);
            BotonL = FindViewById<ImageButton>(Resource.Id.btnL);
            BotonS = FindViewById<ImageButton>(Resource.Id.btnS);

            BotonBlue.Click += (sender, e) =>
            {
                if (Conectado == false)
                {
                    BotonBlue.SetImageDrawable(null);
                    var image = VectorDrawableCompat.Create(this.Resources, Resource.Drawable.BluetoothPrendido, null);
                    BotonBlue.SetImageDrawable(image);
                    Connect();
                }
                else
                {
                    BotonBlue.SetImageDrawable(null);
                    var image = VectorDrawableCompat.Create(this.Resources, Resource.Drawable.BluetoothApagado, null);
                    BotonBlue.SetImageDrawable(image);
                    btSocket.Close();
                }

            };

            BotonU.Click += (sender, e) => {
                writeData((new Java.Lang.String("1")));
            };
            BotonL.Click += (sender, e) => {
                writeData((new Java.Lang.String("2")));
            };
            BotonD.Click += (sender, e) => {
                writeData((new Java.Lang.String("3")));
            };
            BotonR.Click += (sender, e) => {
                writeData((new Java.Lang.String("4")));
            };
            BotonS.Click += (sender, e) => {
                writeData((new Java.Lang.String("5")));
            };

            #region Conexion Para el Bluetooth
            //Asignacion de evento del toggle button
            //BotonBlue.Click += (sender, e) => {
            //    tgConnect_HandleCheckedChange(null, null);
            //};
            //tgConnect.CheckedChange += tgConnect_HandleCheckedChange;
            //Verificamos la disponibilidad del sensor Bluetooth en el dispositivo
            CheckBt();
            #endregion
        }

        private void CheckBt()
        {
            //asignamos el sensor bluetooth con el que vamos a trabajar
            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            //Verificamos que este habilitado
            if (!mBluetoothAdapter.Enable())
            {
                Toast.MakeText(this, "Bluetooth Desactivado",
                    ToastLength.Short).Show();
            }
            //verificamos que no sea nulo el sensor
            if (mBluetoothAdapter == null)
            {
                Toast.MakeText(this,
                    "Bluetooth No Existe o esta Ocupado", ToastLength.Short)
                    .Show();
            }
        }

        void tgConnect_HandleCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                //si se activa el toggle button se incial el metodo de conexion
                Connect();
            }
            else
            {
                //en caso de desactivar el toggle button se desconecta del arduino
                if (btSocket.IsConnected)
                {
                    try
                    {
                        btSocket.Close();
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public void Connect()
        {
            //Iniciamos la conexion con el arduino
            BluetoothDevice device = mBluetoothAdapter.GetRemoteDevice(address);
            System.Console.WriteLine("Conexion en curso" + device);

            //Indicamos al adaptador que ya no sea visible
            mBluetoothAdapter.CancelDiscovery();
            try
            {
                //Inicamos el socket de comunicacion con el arduino
                btSocket = device.CreateRfcommSocketToServiceRecord(MY_UUID);
                //Conectamos el socket
                btSocket.Connect();
                System.Console.WriteLine("Conexion Correcta");
                Conectado = true;
            }
            catch (System.Exception e)
            {
                //en caso de generarnos error cerramos el socket
                Console.WriteLine(e.Message);
                try
                {
                    btSocket.Close();
                }
                catch (System.Exception)
                {
                    System.Console.WriteLine("Imposible Conectar");
                }
                System.Console.WriteLine("Socket Creado");
                Conectado = false;
            }
            //Una vez conectados al bluetooth mandamos llamar el metodo que generara el hilo
            //que recibira los datos del arduino
            beginListenForData();
            //NOTA envio la letra e ya que el sketch esta configurado para funcionar cuando
            //recibe esta letra.
            dataToSend = new Java.Lang.String("e");
            writeData(dataToSend);
        }

        public void beginListenForData()
        {
            //Extraemos el stream de entrada
            try
            {
                inStream = btSocket.InputStream;
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            //Creamos un hilo que estara corriendo en background el cual verificara si hay algun dato
            //por parte del arduino
            Task.Factory.StartNew(() => {
                //declaramos el buffer donde guardaremos la lectura
                byte[] buffer = new byte[1024];
                //declaramos el numero de bytes recibidos
                int bytes;
                while (true)
                {
                    try
                    {
                        //leemos el buffer de entrada y asignamos la cantidad de bytes entrantes
                        bytes = inStream.Read(buffer, 0, buffer.Length);
                        //Verificamos que los bytes contengan informacion
                        if (bytes > 0)
                        {
                            //Corremos en la interfaz principal
                            RunOnUiThread(() => {
                                //Convertimos el valor de la informacion llegada a string
                                string valor = System.Text.Encoding.ASCII.GetString(buffer);
                                //Agregamos a nuestro label la informacion llegada
                                string Lectura = valor;
                                string Res = Lectura.Substring(0, 5);
                                try
                                {
                                    string[] Ultimo = Res.Split('\r', '\n', ' ');
                                    foreach (var val in Ultimo)
                                    {
                                        if (val != "")
                                        {
                                            Valores = double.Parse(val);
                                            ValorFinal = Convert.ToInt16(Valores);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    Valores = 0;
                                }
                            });
                        }
                    }
                    catch (Java.IO.IOException)
                    {
                        //En caso de error limpiamos nuestra label y cortamos el hilo de comunicacion
                        RunOnUiThread(() => {
                            //Result.Text = string.Empty;
                        });
                        break;
                    }
                }
            });
        }

        private void writeData(Java.Lang.String data)
        {
            //Extraemos el stream de salida
            try
            {
                outStream = btSocket.OutputStream;
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Error al enviar" + e.Message);
            }

            //creamos el string que enviaremos
            Java.Lang.String message = data;

            //lo convertimos en bytes
            byte[] msgBuffer = message.GetBytes();

            try
            {
                //Escribimos en el buffer el arreglo que acabamos de generar
                outStream.Write(msgBuffer, 0, msgBuffer.Length);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Error al enviar" + e.Message);
            }
        }
    }
}