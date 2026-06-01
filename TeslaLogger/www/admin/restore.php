<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Restore Database 1.0</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<style>
		#progress-container {
			display: none;
			margin: 20px 0;
			padding: 15px;
			border: 1px solid #ddd;
			border-radius: 5px;
			background-color: #f9f9f9;
		}
		#progress-bar {
			width: 100%;
			height: 25px;
			background-color: #e0e0e0;
			border-radius: 5px;
			overflow: hidden;
			margin-bottom: 10px;
		}
		#progress-fill {
			height: 100%;
			width: 0%;
			background-color: #4CAF50;
			transition: width 0.3s ease;
		}
		#progress-text {
			font-weight: bold;
			margin-bottom: 10px;
		}
		#status-log {
			width: 100%;
			height: 200px;
			padding: 10px;
			font-family: monospace;
			font-size: 12px;
			border: 1px solid #ccc;
			border-radius: 3px;
			resize: vertical;
			background-color: #fff;
		}
		#upload-section {
			margin-top: 20px;
		}
		.restore-complete {
			color: #4CAF50;
			font-weight: bold;
			font-size: 1.1em;
		}
		.restore-error {
			color: #f44336;
			font-weight: bold;
			font-size: 1.1em;
		}
	</style>
  </head>
<body>
<div>
<?php 
    include "menu.php";
    echo(menu("Restore"));
?>
	<h1><?php t("Restore"); ?></h1>
	<p><?php t("TextRestore1"); ?></p>
	<p><?php t("TextRestore2"); ?></p>
	<p><?php t("TextRestore3"); ?></p>
	
	<div id="progress-container">
		<div id="progress-text">Preparing...</div>
		<div id="progress-bar">
			<div id="progress-fill"></div>
		</div>
		<textarea id="status-log" readonly></textarea>
	</div>
	
	<div id="upload-section">
		<form id="restore-form" method="post" enctype="multipart/form-data">
			<input type="file" name="fileToUpload" id="fileToUpload">
			<br><br>
			<input type="submit" value="<?php t("Restore"); ?>" name="submit" id="restore-btn">
		</form>
	</div>
</div>

<script>
var session_id = '';
var pollInterval = null;
var logMessages = [];

function addLog(message) {
	var timestamp = new Date().toLocaleTimeString();
	logMessages.push('[' + timestamp + '] ' + message);
	var logEl = document.getElementById('status-log');
	logEl.value = logMessages.join('\n');
	logEl.scrollTop = logEl.scrollHeight;
}

function updateProgress(data) {
	var progressFill = document.getElementById('progress-fill');
	var progressText = document.getElementById('progress-text');
	
	if (data.progress !== undefined) {
		progressFill.style.width = data.progress + '%';
	}
	
	if (data.message) {
		progressText.textContent = data.message;
		addLog(data.message);
	}
	
	if (data.status === 'completed') {
		clearInterval(pollInterval);
		progressFill.style.backgroundColor = '#4CAF50';
		progressText.innerHTML = '<span class="restore-complete">' + data.message + '</span>';
		addLog('Restore completed! Please reboot the system.');
	} else if (data.status === 'error') {
		clearInterval(pollInterval);
		progressFill.style.backgroundColor = '#f44336';
		progressText.innerHTML = '<span class="restore-error">Error: ' + data.message + '</span>';
		addLog('Error: ' + data.message);
	} else if (data.status === 'not_found') {
		clearInterval(pollInterval);
	}
	
	// Display error logs if available
	if (data.error_log) {
		addLog('MySQL log: ' + data.error_log);
	}
	if (data.script_log) {
		addLog('Script log: ' + data.script_log);
	}
}

function pollProgress() {
	if (!session_id) return;
	
	$.ajax({
		url: 'restore_progress.php',
		method: 'GET',
		data: { session_id: session_id },
		success: function(data) {
			updateProgress(data);
		},
		error: function() {
			addLog('Error polling progress...');
		}
	});
}

$(document).ready(function() {
	$('#restore-form').on('submit', function(e) {
		e.preventDefault();
		console.log('Form submit triggered');
		
		var fileInput = $('#fileToUpload')[0];
		if (!fileInput.files || fileInput.files.length === 0) {
			alert('Please select a file to restore.');
			return;
		}
		
		console.log('File selected: ' + fileInput.files[0].name);
		var formData = new FormData(this);
		$('#restore-btn').prop('disabled', true).val('Restoring...');
		$('#progress-container').show();
		$('#upload-section').hide();
		
		logMessages = [];
		addLog('Starting restore process...');
		
		$.ajax({
			url: 'restore_upload.php',
			method: 'POST',
			data: formData,
			processData: false,
			contentType: false,
			timeout: 300000,
			success: function(data) {
				console.log('Upload response:', data);
				if (data.error) {
					addLog('Server Error: ' + data.error);
					$('#restore-btn').prop('disabled', false).val('<?php t("Restore"); ?>');
					$('#upload-section').show();
					$('#progress-container').hide();
				} else {
					session_id = data.session_id;
					addLog('Restore session started: ' + session_id);
					addLog('Monitoring progress...');
					
					// Start polling immediately and then every 2 seconds
					pollProgress();
					pollInterval = setInterval(pollProgress, 2000);
				}
			},
			error: function(xhr, status, error) {
				console.log('Upload error:', status, error, xhr.responseText);
				addLog('Upload error (' + status + '): ' + error);
				if (xhr.responseText) {
					addLog('Response: ' + xhr.responseText.substring(0, 500));
				}
				$('#restore-btn').prop('disabled', false).val('<?php t("Restore"); ?>');
				$('#upload-section').show();
				$('#progress-container').hide();
			}
		});
	});
});
</script>
</body>
</html>
