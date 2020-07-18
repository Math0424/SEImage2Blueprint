using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using Color = System.Drawing.Color;
using Microsoft.Win32;

namespace SEImage2Blueprint
{
    public partial class MainWindow : Window
    {

        public delegate void UpdateTextCallback(string message);

        private string header = @"<?xml version=""1.0""?>
<Definitions xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <ShipBlueprints>
    <ShipBlueprint xsi:type=""MyObjectBuilder_ShipBlueprintDefinition"">
      <Id Type=""MyObjectBuilder_ShipBlueprintDefinition"" Subtype=""{name}""/>
      <DisplayName>Math0424</DisplayName>
      <CubeGrids>
        <CubeGrid>
          <SubtypeName/>
          <EntityId>118773863687514751</EntityId>
          <PersistentFlags>CastShadows InScene</PersistentFlags>
          <PositionAndOrientation>
            <Position x =""0"" y=""0"" z=""0"" />
            <Forward x=""0"" y=""0"" z=""0"" />
            <Up x=""0"" y=""0"" z=""0"" />
            <Orientation >
              <X>0</X>
              <Y>0</Y>
              <Z>0</Z>
              <W>0</W>
            </Orientation>
          </PositionAndOrientation>
          <GridSizeEnum>{size}</GridSizeEnum>
          <CubeBlocks>";

        private string footer = @"
            </CubeBlocks>
          <DisplayName>{name}</DisplayName>
          <DestructibleBlocks>true</DestructibleBlocks>
          <IsRespawnGrid>false</IsRespawnGrid>
          <LocalCoordSys>0</LocalCoordSys>
          <TargetingTargets />
        </CubeGrid>
      </CubeGrids>
      <WorkshopId>0</WorkshopId>
      <OwnerSteamId>76561198161316860</OwnerSteamId>
      <Points>0</Points>
    </ShipBlueprint>
  </ShipBlueprints>
</Definitions>";

        private string cube = @"
            <MyObjectBuilder_CubeBlock xsi:type=""MyObjectBuilder_CubeBlock"">
              <SubtypeName>{size}BlockArmorBlock</SubtypeName>
              <Min x = ""{x}"" y=""{y}"" z=""0"" />
              <BlockOrientation Forward = ""Down"" Up=""Forward"" />
              <SkinSubtypeId>Clean_Armor</SkinSubtypeId>
              <ColorMaskHSV x = ""{h}"" y=""{s}"" z=""{v}"" />
            </MyObjectBuilder_CubeBlock>";

        private string size = "Large";
        private string imageName;
        private FileInfo imagePath;
        private BitmapSource bitMap;
        private bool running = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private ArrayList ConvertRGB2SE(int r1, int g1, int b1)
        {
            ArrayList list = new ArrayList();
            double r = r1 / 255.0;
            double g = g1 / 255.0;
            double b = b1 / 255.0;
            double mx = Math.Max(r, Math.Max(g, b));
            double mn = Math.Min(r, Math.Min(g, b));
            double df = mx - mn;
            double h = 0;
            double s;
            double v;
            if (mx == mn) {
                h = 0;
            } else if (mx == r) {
                h = (60 * ((g - b) / df) + 360) % 360;
            } else if (mx == g) {
                h = (60 * ((b - r) / df) + 120) % 360;
            } else if (mx == b) {
                h = (60 * ((r - g) / df) + 240) % 360;
            }
            if (mx == 0) {
                s = 0;
            } else {
                s = (df / mx) * 100;
            }
            v = mx * 100;
            list.Add(h / 360);
            list.Add((s - 100) / 100);
            list.Add((v - 25) / 100);
            return list;
        }

        private void UpdateImage(string name)
        {
            imageName = name;
            if (imagePath != null && imagePath.Exists && ImageSize != null)
            {
                double scale = ImageSize.Value;

                BitmapImage image = new BitmapImage(new Uri(imagePath.FullName));

                TransformedBitmap scaledImage = new TransformedBitmap(image, new ScaleTransform(scale, scale));
                ImageImg.Source = scaledImage;

                scaledImage.Freeze();
                bitMap = scaledImage;
                
                ImageSizeLbl.Content = scaledImage.PixelHeight.ToString() + "x" + scaledImage.PixelWidth.ToString();
            }
        }

        private void Convert()
        {
            running = true;
            string blueprintPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SpaceEngineers/Blueprints/local/" + Path.GetFileName(imageName);
            if (Directory.Exists(blueprintPath))
            {
                InfoLbl.Dispatcher.Invoke(() => {
                    InfoLbl.Content = "Blueprint already exists!";
                });
                running = false;
                return;
            }
            Directory.CreateDirectory(blueprintPath);

            File.Copy(imagePath.FullName, blueprintPath + "/thumb.png", overwrite: true);

            StreamWriter text = File.CreateText(blueprintPath + "/bp.sbc");
            text.Write(header.Replace("{size}", size).Replace("{name}", Path.GetFileName(imageName)));

            BitmapHelper.PixelColor[,] color = BitmapHelper.GetPixels(bitMap);

            int total = 0;
            for (int x = 0; x < color.GetLength(0); x++)
            {
                InfoLbl.Dispatcher.Invoke(() => {
                    InfoLbl.Content = "Finished " + x.ToString() + " of " + color.GetLength(0);
                });

                for (int y = 0; y < color.GetLength(1); y++)
                {
                    BitmapHelper.PixelColor c = color[x, y];

                    if (c.Alpha > 10)
                    {
                        ArrayList colorArr = ConvertRGB2SE((int)c.Red, (int)c.Green, (int)c.Blue);

                        total += 1;
                        text.Write(cube.Replace("{size}", size).Replace("{x}", x.ToString()).Replace("{y}", y.ToString())
                            .Replace("{h}", colorArr[0].ToString())
                            .Replace("{s}", colorArr[1].ToString())
                            .Replace("{v}", colorArr[2].ToString()));
                    }
                }
            }
            text.Write(footer.Replace("{name}", Path.GetFileName(imageName)));
            text.Close();

            InfoLbl.Dispatcher.Invoke(() => {
                InfoLbl.Content = "Finished " + total.ToString() + " total blocks";
            });

            running = false;
        }

        private void UpdateText(string text)
        {
            InfoLbl.Content = text;
        }

        private void LargeGridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                UpdateText("Cant change grid size while running!");
                return;
            }
            LargeGridBtn.IsEnabled = false;
            SmallGridBtn.IsEnabled = true;
            size = "Large";
        }

        private void SmallGridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                UpdateText("Cant change grid size while running!");
                return;
            }
            LargeGridBtn.IsEnabled = true;
            SmallGridBtn.IsEnabled = false;
            size = "Small";
        }

        private void ImageSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScaleLbl != null)
            {
                ScaleLbl.Content = "x" + Math.Round(ImageSize.Value, 2);
                UpdateImage(InputTxt.Text);
            } 
        }

        private void InputTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (running)
            {
                UpdateText("Cant change image while running!");
                return;
            }
            UpdateImage(((TextBox)sender).Text);
        }

        private void ConvertBtn_Click(object sender, RoutedEventArgs e)
        {
            if (imagePath == null)
            {
                UpdateText("Invalid image!");
                return;
            }
            if (running)
            {
                UpdateText("Already running!");
                return;
            }
            Thread thread = new Thread(new ThreadStart(Convert));
            thread.Name = "Converter";
            thread.Priority = ThreadPriority.AboveNormal;
            thread.IsBackground = true;
            thread.Start();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Browse Image Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "png",
                Filter = "all files (*.*)|*.*|jpg files (*.jpg)|*.jpg|png files (*.png)|*.png",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InputTxt.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                imagePath = new FileInfo(openFileDialog.FileName);
                UpdateImage(Path.GetFileNameWithoutExtension(openFileDialog.FileName));
            }
        }
    }
}
