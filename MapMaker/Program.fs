open System
open System.Drawing
open System.Drawing.Imaging
open System.Net
open System.IO
open System.Windows.Forms
open System.Threading
open MathNet.Numerics.Distributions


[<EntryPoint>]
do
    let saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                     "map_images")
    let longitudeStart  = 42.779399
    let latitudeStart = 76.097755

    // For random wait times between requests
    let normalDist = new Normal(600.0, 100.0) // appx 300-900 seconds (5-15 minutes)

    for yy in 0 .. 9 do
        let longitude = longitudeStart - (2.60 * (float)yy)

        printfn "Row %i -----------------------------------------------------" yy

        for xx in 0 .. 17 do
            let latitude = latitudeStart + (3.30 * (float)xx)

            let url = "https://maps.googleapis.com/maps/api/staticmap?"
                      + (sprintf "&center=%f, %f" longitude latitude)
                      + "&size=640x640"
                      + "&scale=2"
                      + "&maptype=terrain"
                      + "&zoom=8"

            printfn "%s" url

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
                croppedImg.Save(Path.Combine(saveDirectory, (sprintf "iimg_y%i_x%i.png" yy xx)),
                                ImageFormat.Png) |> ignore
            else
                raise (new Exception())

            // rate limit or Google will blacklist you
            let secondsWait = normalDist.Sample()
            Thread.Sleep((int)(1000.0 * secondsWait))
