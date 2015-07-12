open System
open System.Drawing
open System.Drawing.Imaging
open System.Net
open System.IO
open System.Windows.Forms
open System.Threading


[<EntryPoint>]
do
    let saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                     "map_images")
    let longitudeStart  = 39.62
    let latitudeStart = -104.9

    for yy in 1 .. 4 do
        let longitude = longitudeStart - (2.60 * (float)yy)

        for xx in 1 .. 4 do
            let latitude = latitudeStart + (3.30 * (float)xx)

            let url = "https://maps.googleapis.com/maps/api/staticmap?"
                      + "&" + (sprintf "center=%f, %f" longitude latitude)
                      + "&" + "size=640x640"
                      + "&" + "scale=2"
                      + "&" + "maptype=terrain"
                      + "&" + "zoom=8"

            let mapRequest = HttpWebRequest.CreateHttp(url)
            let response = mapRequest.GetResponse()
            let dataStream = match response.ContentType with
                             | "image/png" -> response.GetResponseStream()
                             | _ -> raise (new Exception())

            let wholeBitmap = Bitmap.FromStream(dataStream) :?> Bitmap

            // crop out Google logo
            let croppedImg = wholeBitmap.Clone(new Rectangle(0, 0, wholeBitmap.Width,
                                                             int(float(wholeBitmap.Height) * 0.957)),
                                               Imaging.PixelFormat.DontCare)

            if Directory.Exists(saveDirectory) then
                croppedImg.Save(Path.Combine(saveDirectory, (sprintf "img_y%i_x%i.png" yy xx)),
                                ImageFormat.Png) |> ignore
            else
                raise (new Exception())

            // rate limit or Google hates you
            Thread.Sleep(3000)
