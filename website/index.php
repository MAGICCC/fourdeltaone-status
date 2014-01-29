<?php

error_reporting(E_ERROR | E_WARNING);

if(empty($_GET["page"]) || !file_exists("pages/".$_GET["page"].".php"))
{
	header("location: status");
	exit;
}

define("ROOT", dirname(__FILE__));

require_once("includes/html_api.php");
require_once("menu.cfg");

function gen_leds($titles)
{
?>
	<table style="width: 100%">
		<? foreach($titles as $id=>$title) { ?>
		<tr>
			<td class="status-led led-td-<?=$id ?>"><img id="led-<?=$id ?>" width="48" height="48" title="<?=$title ?>" alt="<?=$title ?>" src="http://cdn1.iconfinder.com/data/icons/softwaredemo/PNG/48x48/Box_Grey.png" /></td>
			<td class="status-text text-<?=$id ?>" style="font-size: 16px; text-align: left"><?=htmlentities($title) ?></td>
		</tr>
		<? } ?>
	</table>
<? }

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<? include("templates/meta.html"); ?>
<title>fourdeltaone.<?=$_GET["page"] ?></title>
<? include("templates/fonts.html"); ?>
<? include("templates/styles.html"); ?>
<? include("templates/scripts.html"); ?>
</head>

<body class="gradient">
	<div class="headbar gradient">
		<div class="headlogo">
			<span id="fdocol">fourdeltaone</span>.<?=$_GET["page"] ?>
		</div>
		<span style="font-size: 11px" id="fdoss"><!-- --></span>
<!--	<span style="font-size: 11px"><a href ="*Homepage to the second server here*">Switch Server</a></span> <!-- When you have more than one backend running, you can change the servers here -->
		<?php menu(); ?>
	</div>

	<? require("pages/".$_GET["page"].".php"); ?>

	<div style="text-align: center; font-size: 8px; padding-top: 50px; color: #AAAAAA">
		<p>This status page is being maintained by <a href ="" target="_blank"></a> now. &copy; <?=date("Y") ?> MAGIC.</p> <!-- Do not remove the copyright -->
	</div>
    </body>
</html>