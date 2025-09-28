# Reverse Proxy Configuration for Teslalogger

This guide describes how to configure a reverse proxy to access Teslalogger and Grafana via subdomains, including automatic login for Grafana.

## Prerequisites

This example assumes the following setup:
- Reverse Proxy: npm (or npm plus)
- Domain: `*.example.org`
- Teslalogger IP: `10.0.0.10` (or `raspberry`)
- Teslalogger Port: `8888`
- Grafana Port: `3000`
- Grafana Username: `admin`
- Grafana Password: `teslalogger`

## Goal

- `httptesla.example.org` -> Direct access to Teslalogger (without IP, without /admin)
- `teslastats.example.org` -> Direct access to Grafana (without login)

## Teslalogger Configuration

1. Create a new proxy host
2. Set the following values under "Details":
   - Domain Name: `tesla.example.org`
   - Scheme: `http`
   - IP: `10.0.0.10`
   - Port: `8888`
   - Enable Websockets and all checkboxes
3. Under "TLS" (or "Certificate"), select the `*.example.org` certificate and enable all checkboxes
4. Under "Custom" (or "Advanced"), enter the following to automatically redirect to the "/admin" directory:
   ```nginx
   location / {
       proxy_pass http://10.10.0.10:5500/admin/;
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
       proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
       proxy_set_header X-Forwarded-Proto $scheme;
   }
   ```
5. Save the configuration

Teslalogger should now be accessible at `https://tesla.example.org`. (Test in private browsing mode to avoid cache issues)

## Grafana Configuration

1. Create a new proxy host
2. Set the following values under "Details":
   - Domain Name: `teslastats.example.org`
   - Scheme: `http`
   - IP: `10.0.0.10`
   - Port: `3000`
   - Enable Websockets and all checkboxes
3. Under "TLS" (or "Certificate"), select the `*.example.org` certificate and enable all checkboxes
4. Under "Custom" (or "Advanced"), enter the following for automatic login:
   ```nginx
   location / {
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
       proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
       proxy_set_header X-Forwarded-Proto $scheme;
       
       # Direct with hardcoded Credentials
       proxy_set_header Authorization "Basic YWRtaW46dGVzbGFsb2dnZXI=";
       
       proxy_pass http://10.0.0.10:3000;
   }
   ```
5. Save the configuration

Grafana should now be accessible at `https://teslastats.example.org`. (Test in private browsing mode to avoid cache issues)

## Teslalogger Settings

To ensure proper linking in Teslalogger, set the following values under "Settings":
- URL Admin Panel: `https://tesla.example.org`
- URL Grafana: `https://tesla.example.org`

Setup complete. 

## Additional Information: Generate Basic Auth Header

The Basic Auth header used in the Grafana configuration (`YWRtaW46dGVzbGFsb2dnZXI=`) is a Base64 encoded string of `admin:teslalogger`. You can generate this value using one of these methods:

1. Using Linux/macOS terminal:
   ```bash
   echo -n "admin:teslalogger" | base64
   ```

2. Using PowerShell:
   ```powershell
   [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("admin:teslalogger"))
   ```

3. Using Python:
   ```python
   import base64
   print(base64.b64encode("admin:teslalogger".encode()).decode())
   ```

4. Using an online tool:
   - Go to https://www.base64encode.org/
   - Enter `admin:teslalogger`
   - Click encode
   - The result should be `YWRtaW46dGVzbGFsb2dnZXI=`

Replace `admin:teslalogger` with your actual username:password combination if you use different credentials.

