# ff-link-grabber

A Windows desktop utility for batch downloading files from fuckingfast.co links.

## What it does

The application parses fuckingfast URLs from text input, identifies filenames, and automatically groups multipart archives such as multi-part RAR files into packs. It uses an embedded WebView2 browser to pass Turnstile verification and trigger direct file downloads into a local folder.

You can set concurrency limits, add delay between extraction requests, monitor download speed and progress, and pause, resume, or skip individual items.

## Requirements

Windows 10 or 11 with .NET 9.0 and Microsoft Edge WebView2 runtime installed.

## How to use

Open the solution in Visual Studio or build it with dotnet build. Launch the application, paste your links into the input box, and click Parse. Select your destination folder, adjust the parallel download limit and delay if needed, then click Start.
