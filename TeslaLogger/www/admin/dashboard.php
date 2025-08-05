<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
session_start();
$carid = GetDefaultCarId();
if (isset($_REQUEST["carid"]))
{
	$_SESSION["carid"] = $_REQUEST["carid"];
	$carid = $_REQUEST["carid"];
}
else
{
	$_SESSION["carid"] = $carid;
}

?>
<html lang="<?php echo $json_data["Language"]; ?>">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<meta name="apple-mobile-web-app-title" content="Teslalogger Dashboard">
	<link href="manifest.json" rel="manifest" crossorigin="use-credentials">
	<link rel="icon" type="image/png" href="img/apple-touch-icon.png" sizes="131x133">
	<link rel="icon" type="image/png" href="img/apple-touch-icon-192.png" sizes="192x192">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger <?php t("Dashboard"); ?></title>
	<link rel="stylesheet" href="dashboard.css?v=<?=time()?>" />
	<link rel="stylesheet" href="my_dashboard.css?v=<?=time()?>" />
	<link href='https://fonts.googleapis.com/css?family=Roboto:300,400,500,300italic' rel='stylesheet' type='text/css'>
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script>

	var nextGetWeather = 0;
	var loc;

	$( function() {
	<?php
		$path = "wallpapers/".$carid;
		if (isDocker())
		{
			echo("$('#error').text('');\n");
			echo("$('body').css('background-image','url(\"dashboard-pic.php?carid=".$carid."\")');\n");
			echo("checkWallpaperExists('dashboard-pic.php?carid=".$carid."');\n");
		}
		else
		{
			$files = scandir($path);
			$wallpaperFound = false;
			foreach ($files as $i => &$f)
			{
				if (stripos($f,".") === 0)
					continue;

				if (stripos($f,".jpg") > 0 || stripos($f,".png") > 0)
				{
					echo("$('#error').text('');\n");
					echo("$('body').css('background-image','url(\"tmp/wallpapers/".$carid."/".$f. "\")');\n");
					$wallpaperFound = true;
					break;
				}
			}
			
			if (!$wallpaperFound) {
				echo("// No wallpaper found, show upload dialog after a short delay\n");
				echo("setTimeout(function() { showUploadDialog(); }, 2000);\n");
			}
		}
	
	?>
		if (navigator.languages != undefined) loc = navigator.languages[0];
			else loc = navigator.language;

		GetCurrentData();

		setInterval(function()
		{
			GetCurrentData();
		}
		,5000);

		// Settings menu toggle functionality
		toggleSettingsMenu();
		$('#settings img').click(function() {
			toggleSettingsMenu();
		});
	} );

	function GetCurrentData()
	{
		updateClock();
		updateWeather();

		$.ajax({
		  url: "current_json.php?carid=<?php echo $carid; ?>",
		  dataType: "json"
		  }).done(function( jsonData ) {
			$('#display_name').text(jsonData["display_name"]);
			$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toFixed(0));
			$('#battery_level').text(jsonData["battery_level"]);

			updateBat(jsonData["battery_level"]);

			if (jsonData["charging"])
			{
				$('#car_statusLabel').text("<?php t("Charging"); ?>:");
				$('#car_status').html(jsonData["charger_power"] + " kW / +" + jsonData["charge_energy_added"] + " kWh");
			}
			else if (jsonData["driving"])
			{
				$('#car_statusLabel').text("<?php t("Driving"); ?>:");
				$('#car_status').text(jsonData["speed"] + " km/h / " + jsonData["power"]+"PS");
			}
			else if (jsonData["online"])
			{
				var text = "<?php t("Online"); ?>";

				if (jsonData["is_preconditioning"])
					text = text + "<br><?php t("Preconditioning"); ?> "+ jsonData["inside_temperature"] +"°C";

				if (jsonData["sentry_mode"])
					text = text + "<br><?php t("Sentry Mode"); ?>";

				if (jsonData["battery_heater"])
					text = text + "<br><?php t("Battery Heater"); ?>";

				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').html(text);
			}
			else if (jsonData["sleeping"])
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Sleeping"); ?>");
			}
			else if (jsonData["falling_asleep"])
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Falling asleep"); ?>");
			}
			else
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Offline"); ?>");
			}

<!-- Begin of my_dashboard_jsonData.php -->
<?PHP
if (file_exists("my_dashboard_jsonData.php"))
	include("my_dashboard_jsonData.php");
?>
<!-- End of my_dashboard_jsonData.php -->

		});
	}

	function updateClock()
	{
		var currentTime = new Date ( );

		var currentHours = currentTime.getHours ( );
		var currentMinutes = currentTime.getMinutes ( );
		var currentSeconds = currentTime.getSeconds ( );

		currentMinutes = ( currentMinutes < 10 ? "0" : "" ) + currentMinutes;
		var currentTimeString = currentHours + ":" + currentMinutes;

		$('#clock').text(currentTimeString);
		$('#weekday').text(currentTime.toLocaleString(loc, { weekday: 'long'}));
		$('#date').text(currentTime.toLocaleString(loc, { month: 'long', day: 'numeric' }));
	}

	function updateBat($percent)
	{
		$batwidth = $('#batimg').width();
		$batheight = $('#batimg').height();
		$('#batimg_end').css('height', $batheight);
		$('#batimg_m').css('height', $batheight );

		$newwidth = $batwidth * 0.92 * $percent / 100;

		$topspace = 4;
		$leftspace = 7;

		if ($batwidth < 150)
		{
			$topspace = 8;
			$leftspace = 3;
		}
		else if ($batwidth < 200)
		{
			$topspace = 7;
			$leftspace = 5;
		}

		$('#batimg_m').css('top', $topspace);
		$('#batimg_end').css('top', $topspace);

		$('#batimg_m').css('width', $newwidth);
		$('#batimg_m').css('left', $leftspace);
		$('#batimg_end').css('left', $newwidth + $leftspace);

	}

	function updateWeather()
	{
		if (nextGetWeather > Date.now())
			return;

		nextGetWeather = Date.now() + 600000;

		<?php
		if (isDocker())
			$weahterinifile = "/tmp/teslalogger/weather.ini";
		else
			$weahterinifile = "/etc/teslalogger/weather.ini";

		if (file_exists($weahterinifile))
		{
			$weatherParams = parse_ini_file($weahterinifile);

			if ($weatherParams['appid'] =="12345678901234567890123456789012")
			{
				echo("$('#weather').css('display','none');return; <!-- default weather.ini file! Disable Weather Widget -->");
			}
			else
				echo("const weatherUrl='//api.openweathermap.org/data/2.5/forecast?q=" . $weatherParams['city'] . "&units=metric&APPID=" . $weatherParams['appid'] . "';");
		}
		else
		{
			echo("$('#weather').css('display','none');return; <!-- weather.ini file not found! Disable Weather Widget -->");
		}
		?>

		var minTemp = 100;
		var maxTemp = -100;
		var weatherIcon = "partlycloudy.png";
		var forecast = 'few clouds';

		var now = new Date();
		var dateForecast = new Date();
		var dateTemp = new Date();

		if(now.getHours() <= 14) dateForecast.setHours(15,0,0,0);
			else dateForecast.setHours(18,0,0,0);
		if(now.getHours() >= 18) {
			dateForecast.setDate(dateForecast.getDate() + 1);
			dateForecast.setHours(15,0,0,0);
		}

		$.getJSON(weatherUrl, function(weatherData){

			var i = 0;
			while(weatherData.list[i].dt < Math.round(dateForecast.getTime()/1000)) { i++;	}

			forecast = weatherData.list[i].weather[0].main;
			console.log("forecast: " + forecast);
			console.log(weatherData.list[i].dt_txt);
			switch (forecast) {
				case 'Thunderstorm':
				weatherIcon = "tstorms.png";
				break;
				case 'Drizzle':
				weatherIcon = "flurries.png";
				break;
				case 'Rain':
				weatherIcon = "rain.png";
				break;
				case 'Snow':
				weatherIcon = "snow.png";
				break;
				case 'Atmosphere':
				weatherIcon = "hazy.png";
				break;
				case 'Clear':
				weatherIcon = "clear.png";
				break;
				case 'Clouds':
				weatherIcon = "cloudy.png";
				break;
				default:
				weatherIcon = "unknown.png";
			}

			i = 0;

			if(dateTemp.getHours() <= 16) {
				dateTemp.setDate(dateTemp.getDate() + 1);
				dateTemp.setHours(1,0,0,0);
				while(weatherData.list[i].dt <= Math.round(dateTemp.getTime()/1000)) {
					if(weatherData.list[i].main.temp_min < minTemp) minTemp = weatherData.list[i].main.temp_min;
					if(weatherData.list[i].main.temp_max > maxTemp) maxTemp = weatherData.list[i].main.temp_max;
					i++;
				}
			}
			else {
				dateTemp.setDate(dateTemp.getDate() + 1);
				dateTemp.setHours(1,0,0,0);
				while(weatherData.list[i].dt <= Math.round(dateTemp.getTime()/1000)) i++;
				while(weatherData.list[i].dt <= Math.round(dateTemp.getTime()/1000) + 86400) {
					if(weatherData.list[i].main.temp_min < minTemp) minTemp = weatherData.list[i].main.temp_min;
					if(weatherData.list[i].main.temp_max > maxTemp) maxTemp = weatherData.list[i].main.temp_max;
					i++;
				}
			}

			$('#temp').text(Math.round(minTemp) + "°C / " + Math.round(maxTemp) + "°C");
			$("#weather_icon").attr("src", "img/weather/" + weatherIcon);
		}).error(function(error)
		{
			$('#temp').text(error.responseText);
		});
	}

  function BackgroudRun($target, $text)
  {
	  $.ajax($target, {
		data: {
			id: ''
		}
		})
		.then(
		function success(name) {
			alert($text);
		},
		function fail(data, status) {
			alert($text);
		}
	);
  }

  function toggleSettingsMenu()
  {
	  $('#settingsmenu').toggle();
  }

  function showUploadDialog()
  {
	  $('#uploadDialog').show();
	  $('#settingsmenu').hide();
  }

  function hideUploadDialog()
  {
	  $('#uploadDialog').hide();
  }

  function showWeatherDialog()
  {
	  // Load current weather settings
	  $.ajax({
		  url: 'set_weather_key.php',
		  type: 'GET',
		  success: function(response) {
			  if (response.configured) {
				  $('#weatherCity').val(response.city);
				  // Don't pre-fill API key for security reasons
			  }
		  },
		  error: function() {
			  // Ignore errors when loading current settings
		  }
	  });
	  
	  $('#weatherDialog').show();
	  $('#settingsmenu').hide();
  }

  function hideWeatherDialog()
  {
	  $('#weatherDialog').hide();
  }

  function checkWallpaperExists(imageUrl)
  {
	  // Method 1: Using Image object
	  var img = new Image();
	  img.onload = function() {
		  // Image loaded successfully, do nothing
		  console.log('Wallpaper loaded successfully');
	  };
	  img.onerror = function() {
		  // Image failed to load (404 or other error)
		  console.log('Wallpaper not found (404), showing upload dialog');
		  // Show upload dialog after a short delay
		  setTimeout(function() {
			  showUploadDialog();
		  }, 2000);
	  };
	  img.src = imageUrl + '&t=' + new Date().getTime(); // Add timestamp to prevent caching
	  
	  // Method 2: Alternative AJAX check for more reliable 404 detection
	  $.ajax({
		  url: imageUrl,
		  type: 'HEAD', // Only get headers, not the full image
		  cache: false,
		  success: function() {
			  console.log('Wallpaper exists (AJAX check)');
		  },
		  error: function(xhr) {
			  if (xhr.status === 404) {
				  console.log('Wallpaper 404 confirmed via AJAX, showing upload dialog');
				  setTimeout(function() {
					  showUploadDialog();
				  }, 2000);
			  }
		  }
	  });
  }

  // Handle wallpaper upload form submission
  $(document).ready(function() {
	  $('#wallpaperForm').on('submit', function(e) {
		  e.preventDefault();
		  
		  var fileInput = $('#wallpaperFile')[0];
		  if (!fileInput.files.length) {
			  alert('Please select a file to upload.');
			  return;
		  }
		  
		  var formData = new FormData(this);
		  
		  $.ajax({
			  url: 'upload_wallpaper.php',
			  type: 'POST',
			  data: formData,
			  processData: false,
			  contentType: false,
			  success: function(response) {
				  try {
					  var result = JSON.parse(response);
					  if (result.success) {
						  alert('Wallpaper uploaded successfully!');
						  hideUploadDialog();
						  // Reload the page to show the new wallpaper
						  location.reload();
					  } else {
						  alert('Upload failed: ' + result.error);
					  }
				  } catch (e) {
					  alert('Upload failed: Invalid response from server');
				  }
			  },
			  error: function(xhr, status, error) {
				  try {
					  var result = JSON.parse(xhr.responseText);
					  alert('Upload failed: ' + result.error);
				  } catch (e) {
					  alert('Upload failed: ' + error);
				  }
			  }
		  });
	  });

	  // Handle weather API key form submission
	  $('#weatherForm').on('submit', function(e) {
		  e.preventDefault();
		  
		  var apiKey = $('#weatherApiKey').val().trim();
		  var city = $('#weatherCity').val().trim();
		  
		  if (!apiKey || !city) {
			  alert('Please fill in both API key and city.');
			  return;
		  }
		  
		  var formData = {
			  api_key: apiKey,
			  city: city
		  };
		  
		  $.ajax({
			  url: 'set_weather_key.php',
			  type: 'POST',
			  data: formData,
			  success: function(response) {
				  try {
					  var result = JSON.parse(response);
					  if (result.success) {
						  alert('Weather API key saved successfully!');
						  hideWeatherDialog();
						  // Reload the page to apply new weather settings
						  location.reload();
					  } else {
						  alert('Save failed: ' + result.error);
					  }
				  } catch (e) {
					  alert('Save failed: Invalid response from server');
				  }
			  },
			  error: function(xhr, status, error) {
				  try {
					  var result = JSON.parse(xhr.responseText);
					  alert('Save failed: ' + result.error);
				  } catch (e) {
					  alert('Save failed: ' + error);
				  }
			  }
		  });
	  });
  });
  </script>
</head>
<body>
<div id="panel">
	  <div id="headline"><span id="display_name"> </span><span id="teslalogger">Teslalogger Dashboard</span></div>
	  <div id="rangeline"><span id="batdiv"><img id="batimg" src="img/bat-icon.png"><img id="batimg_m" src="img/bat-icon-gr.png"><img id="batimg_end" src="img/bat-icon-end.png"></span>
	  <span id="battery_level" style="">-</span><font id="percent">%</font> <span id="tilde">~</span> <span id="ideal_battery_range_km" style="">-</span><font id="km">km</font></div>
	  <div id="car_statusLabel">-</div>
	  <div id="car_status">-</div>
	  <div id="error">
<?PHP
	  	if (isDocker())
			echo ("No wallpapers found in \\TeslaLogger\\www\\admin\\wallpapers\\$carid\\");
		else
			echo ("No wallpapers found in \\\\raspberry\\teslalogger-web\\admin\\wallpapers\\$carid\\");
?>
		</div>
  </div>

	<div id="calendar">
		<span id="weekday"></span><br>
		<span id="date"></span><br>
		<span id="clock">00:00</span>
	</div>

  <div id="weather">
	<img id="weather_icon" src="">
	<div id="temp">no weather data</div>
  </div>

  <div id="settings">
	<div><img src="img/gear.png" align="right"> </div>
	<span id="settingsmenu">
		<h1>Settings</h1>
		<a href="#" onclick="showUploadDialog()">Upload Wallpaper</a><br>
		<a href="#" onclick="showWeatherDialog()">Set Weather Key</a><br>
	</span>
  </div>

  <div class="dialog" id="uploadDialog" style="display: none;">
	<h1>Wallpaper Upload</h1>
	<p>Upload a new wallpaper for your car.</p>
	<form id="wallpaperForm" enctype="multipart/form-data">
		<input type="file" id="wallpaperFile" name="wallpaper" accept="image/jpeg,image/jpg,image/png" required>
		<input type="hidden" name="carid" value="<?php echo $carid; ?>">
		<br><br>
		<button type="button" onclick="hideUploadDialog()">Cancel</button>
		<button type="submit">Upload</button>
	</form>
  </div>

  <div class="dialog" id="weatherDialog" style="display: none;">
	<h1>Weather API Configuration</h1>
	<p>Configure OpenWeatherMap API settings for weather display.</p>
	<form id="weatherForm">
		<label for="weatherApiKey">API Key:</label><br>
		<input type="text" id="weatherApiKey" name="api_key" placeholder="Enter your OpenWeatherMap API key" required maxlength="32"><br><br>
		
		<label for="weatherCity">City:</label><br>
		<input type="text" id="weatherCity" name="city" placeholder="Enter your city name" required><br><br>
		
		<p style="font-size: 12px; color: #ccc;">
			Get your free API key at: <a href="https://openweathermap.org/api" target="_blank" style="color: #007ACC;">openweathermap.org/api</a>
		</p>
		
		<button type="button" onclick="hideWeatherDialog()">Cancel</button>
		<button type="submit">Save</button>
	</form>
  </div>

<!-- Begin of my_dashboard.php -->

<?PHP
if (file_exists("my_dashboard.php"))
	include("my_dashboard.php");
?>

<!-- End of my_dashboard.php -->
</body>
</html>
