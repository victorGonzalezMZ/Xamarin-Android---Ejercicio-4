using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Plugin.CurrentActivity;
using Plugin.Media;
using System;
using System.IO;

namespace Ejercicio4App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string Archivo;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            SupportActionBar.Hide();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
            var Imagen = FindViewById<ImageView>(Resource.Id.image);
            var btnAlmacenar = FindViewById<Button>(Resource.Id.btnSave);
            var txtNombre = FindViewById<EditText>(Resource.Id.txtNombre);
            var txtDomicilio = FindViewById<EditText>(Resource.Id.txtDomicilio);
            var txtCorreo = FindViewById<EditText>(Resource.Id.txtCorreo);
            var txtEdad = FindViewById<EditText>(Resource.Id.txtEdad);
            var txtSaldo = FindViewById<EditText>(Resource.Id.txtSaldo);

            Imagen.Click += async delegate
            {
                await CrossMedia.Current.Initialize();
                var archivo = await CrossMedia.Current.TakePhotoAsync
                (new Plugin.Media.Abstractions.StoreCameraMediaOptions 
                {
                    Directory = "Imagenes",
                    Name = txtNombre.Text,
                    SaveToAlbum = true,
                    CompressionQuality = 30,
                    CustomPhotoSize = 30,
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                    DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Rear
                });
                if (archivo == null)
                    return;
                Bitmap bp = BitmapFactory.DecodeStream(archivo.GetStream());
                Archivo = System.IO.Path.Combine(System.Environment.GetFolderPath
                    (System.Environment.SpecialFolder.Personal),
                    txtNombre.Text + ".jpg");
                var stream = new FileStream(Archivo, FileMode.Create);
                bp.Compress(Bitmap.CompressFormat.Jpeg, 30, stream);
                stream.Close();
                Imagen.SetImageBitmap(bp);
                long memoria1 = GC.GetTotalMemory(false);
                Toast.MakeText(this, memoria1.ToString(), ToastLength.Long).Show();
                GC.Collect();
                long memoria2 = GC.GetTotalMemory(false);
                Toast.MakeText(this, memoria2.ToString(), ToastLength.Long).Show();
            };

            btnAlmacenar.Click += async delegate
            {
                try
                {
                    var CuentadeAlmacenamiento = CloudStorageAccount.Parse
                    ("DefaultEndpointsProtocol=https;AccountName=azurevictoralm123;AccountKey=T6lYyS7R9VV2ppmVjM+Cch4kNxgtFITnOUPRIdCkC5ydL2ogrOlzoCIHE2OTWTXg4kr63vQMJ6OdvhRnu2wKTw==;EndpointSuffix=core.windows.net"); // Azure Account Key
                    var ClienteBlob = CuentadeAlmacenamiento.CreateCloudBlobClient();
                    var Carpeta = ClienteBlob.GetContainerReference("victor");
                    var resourceBlob = Carpeta.GetBlockBlobReference(txtNombre.Text + ".jpg");
                    await resourceBlob.UploadFromFileAsync(Archivo.ToString());
                    Toast.MakeText(this, "Imagen Almacenada en contenedor de blobs",
                        ToastLength.Long).Show();
                    var TablaNoSQL = CuentadeAlmacenamiento.CreateCloudTableClient();
                    var Coleccion = TablaNoSQL.GetTableReference("Registro");
                    await Coleccion.CreateIfNotExistsAsync();
                    var cliente = new Clientes("Clientes", txtNombre.Text);
                    cliente.Correo = txtCorreo.Text;
                    cliente.Saldo = double.Parse(txtSaldo.Text);
                    cliente.Edad = int.Parse(txtEdad.Text);
                    cliente.Domicilio = txtDomicilio.Text;
                    cliente.ImagenBlob = txtNombre.Text + ".jpg";
                    var Store = TableOperation.Insert(cliente);
                    await Coleccion.ExecuteAsync(Store);
                    Toast.MakeText(this, "Datos guardados en Table NoSQL en Azure", 
                        ToastLength.Long).Show();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
            };
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    public class Clientes: TableEntity
    {
        public Clientes (string Categoria, string Nombre)
        {
            PartitionKey = Categoria;
            RowKey = Nombre;
        }

        public string Correo { get; set; }
        public string Domicilio { get; set; }
        public int Edad { get; set; }
        public double Saldo { get; set; }
        public string ImagenBlob { get; set; }
    }
}