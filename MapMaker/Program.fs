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

    if not (Directory.Exists(saveDirectory)) then raise (new Exception("Save folder must exist"))

    let longitudeStart  = 45.712660
    let latitudeStart = 58.397553

    // these step values are perfectly spaced for this zoom level *at tile 0,0* but are too spaced at other
    // latitudes. let's use 90% so there is overlap, then we can fix the overlap later.
    let longitudeStep = -2.517 * 0.9
    let latitudeStep = 3.517 * 0.9

    let edgePixels = 640  // maximum value without a business license
    let scale = 2  // but render the vector at twice the pixel density
    let zoom = 8

    // for random wait times between requests
    let normalDistWait = new Normal(75.0, 15.0)

    // sequence of all URLs we will request
    let urls = seq {
        for yy in 0 .. 12 do
            let longitude = longitudeStart + (longitudeStep * (float)yy)

            for xx in 0 .. 20 do
                let latitude = latitudeStart + (latitudeStep * (float)xx)

                let url = "https://maps.googleapis.com/maps/api/staticmap?"
                          + (sprintf "&center=%f, %f" longitude latitude)
                          + (sprintf "&size=%dx%d" edgePixels edgePixels)
                          + (sprintf "&scale=%d" scale)
                          + (sprintf "&zoom=%d" zoom)
                          + "&maptype=terrain"
                yield (url, xx, yy)
    }

    for (url, xx, yy) in urls do
        printf "%d,%d | %s" xx yy url

        let mapRequest = HttpWebRequest.CreateHttp(url)
        let response = mapRequest.GetResponse()
        let dataStream = match response.ContentType with
                            | "image/png" -> response.GetResponseStream()
                            | _ -> raise (new Exception())

        let wholeBitmap = Bitmap.FromStream(dataStream) :?> Bitmap

        // crop out Google logo (sorry!)
        let croppedImg = wholeBitmap.Clone(new Rectangle(0, 0, wholeBitmap.Width,
                                                            int(float(wholeBitmap.Height) * 0.957)),
                                            Imaging.PixelFormat.DontCare)

        // ensure images are alphabetically sortable ("001" not just "1")
        let filename = sprintf "img_y%03i_x%03i.png" yy xx
        croppedImg.Save(Path.Combine(saveDirectory, filename), ImageFormat.Png) |> ignore

        // rate limit or Google will blacklist you
        let secondsWait = normalDistWait.Sample()
        printfn " | Sleeping %f seconds..." secondsWait
        Thread.Sleep((int)(1000.0 * secondsWait))
