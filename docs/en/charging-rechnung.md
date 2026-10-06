# Download Supercharging invoices
After a Supercharger session, Teslalogger regularly checks whether Tesla has already generated the invoice for this charging session and downloads it as soon as it becomes available. In addition, Teslalogger downloads all invoices that are not yet present locally once a day.

They can be found at the following location using the Windows File Explorer:

`\\raspberry\teslalogger\tesla_invoices`

Enter «pi» as the username and «teslalogger» as the password when prompted.

If, as described elsewhere, Teslalogger is run under a different name instead of «raspberry», use the changed name accordingly.

With a Docker installation, the invoices are stored in a `tesla_invoices` subfolder next to the application inside the container (in the container: `/TeslaLogger/bin/tesla_invoices`).

Please do not delete the (already copied) files there, as they would otherwise be downloaded again the next night.
