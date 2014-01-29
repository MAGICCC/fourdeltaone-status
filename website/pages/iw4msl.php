<div id="content" style="text-align: center">
<h2>IW4M Server List</h2>
<?php
require ROOT."/data/iw4m.maps";
require ROOT."/data/iw4m.gametypes";

$serverlist_file =
	file_exists("/srv/iw4msl/serverinfo.txt") ? "/srv/iw4msl/serverinfo.txt"
	: ROOT . "/../../iw4msl/serverinfo.txt"
	;

if(!file_exists($serverlist_file))
{ ?>
<? } else {

function parse_flag($country)
{
	switch(strtolower($country))
	{
		case "hk":
			return "http://upload.wikimedia.org/wikipedia/commons/thumb/5/5b/Flag_of_Hong_Kong.svg/30px-Flag_of_Hong_Kong.svg.png";
		default:
			return "http://flagpedia.net/data/flags/mini/".strtolower($country).".png";
	}
}

function parse_gametype($gt)
{
	global $_GAMETYPES;
	if(empty($_GAMETYPES["iw4m"][$gt]))
		return $gt;
	else
		return $_GAMETYPES["iw4m"][$gt];
}

function parse_gamecolors($string)
{
	$string = htmlentities($string);

	$string = ereg_replace("(\^)?$", "^", $string);
	$string = ereg_replace("\^\^", "&#94^", $string);
	$string = ereg_replace("(\^)?$", "", $string);
	$string = ereg_replace("\^<", "^|", $string);
	$string = ereg_replace("\<", "<", $string);
	$string = ereg_replace("\^>", "^^", $string);
	$string = ereg_replace("\>", ">", $string);
	$string = ereg_replace("\^\^", "^>", $string);
	$string = "<font color=\"#FFFFFF\">".$string."</font>";

	$color_def = array
	(
		0  => "#000000",	1  => "#FF0000",	2  => "#00FF00",	3  => "#FFFF00",
		4  => "#0000FF",	5  => "#00FFFF",	6  => "#FF00FF",	7  => "#FFFFFF",
		8  => "#FF7F00",	9  => "#7F7F7F",	10 => "#BFBFBF",	11 => "#007F00",
		12 => "#7FFF00",	13 => "#00007F",	14 => "#7F0000",	15 => "#7F4000",
		16 => "#FF9933",	17 => "#007F7F",	18 => "#7F007F",	19 => "#007F7F",
		20 => "#7F00FF",	21 => "#3399CC",	22 => "#CCFFCC",	23 => "#006633",
		24 => "#FF0033",	25 => "#B21919",	26 => "#993300",	27 => "#CC9933",
		28 => "#999933",	29 => "#FFFFBF",	30 => "#FFFF7F"
	);

	$color_chardef = array
	(
		"#000000"	=> array (	0 => "0",	1 => "P",	2 => "p"	),
		"#FF0000"	=> array (	0 => "1",	1 => "Q",	2 => "q"	),
		"#00FF00"	=> array (	0 => "2",	1 => "R",	2 => "r"	),
		"#FFFF00"	=> array (	0 => "3",	1 => "S",	2 => "s"	),
		"#0000FF"	=> array (	0 => "4",	1 => "T",	2 => "t"	),
		"#00FFFF"	=> array (	0 => "5",	1 => "U",	2 => "u"	),
		"#FF00FF"	=> array (	0 => "6",	1 => "V",	2 => "v"	),
		"#FFFFFF"	=> array (	0 => "7",	1 => "W",	2 => "w"	),
		"#FF7F00"	=> array (	0 => "8",	1 => "X",	2 => "x"	),
		"#7F7F7F"	=> array (	0 => "9",	1 => "Y",	2 => "y"	),
		"#BFBFBF"	=> array (	0 => ":",	1 => "Z",	2 => "z",
						3 => ";",	4 => "[",	5 => "{"	),
		"#007F00"	=> array (	0 => "<",	1 => "\\",	2 => "|"	),
		"#7FFF00"	=> array (	0 => "=",	1 => "]",	2 => "}"	),
		"#00007F"	=> array (	0 => ">"					),
		"#7F0000"	=> array (	0 => "?"					),
		"#7F4000"	=> array (	0 => "@",	1 => "`"			),
		"#FF9933"	=> array (	0 => "A",	1 => "a",	2 => "!"	),
		"#007F7F"	=> array (	0 => "B",	1 => "b"			),
		"#7F007F"	=> array (	0 => "C",	1 => "c",	2 => "#"	),
		"#007F7F"	=> array (	0 => "D",	1 => "d",	2 => "$"	),
		"#7F00FF"	=> array (	0 => "E",	1 => "e",	2 => "%"	),
		"#3399CC"	=> array (	0 => "F",	1 => "f",	2 => "&"	),
		"#CCFFCC"	=> array (	0 => "G",	1 => "g",	2 => "'"	),
		"#006633"	=> array (	0 => "H",	1 => "h",	2 => "("	),
		"#FF0033"	=> array (	0 => "I",	1 => "i",	2 => ")"	),
		"#B21919"	=> array (	0 => "J",	1 => "j"			),
		"#993300"	=> array (	0 => "K",	1 => "k",	2 => "+"	),
		"#CC9933"	=> array (	0 => "L",	1 => "l",	2 => ","	),
		"#999933"	=> array (	0 => "M",	1 => "m",	2 => "-"	),
		"#FFFFBF"	=> array (	0 => "N",	1 => "n",	2 => "."	),
		"#FFFF7F"	=> array (	0 => "O",	1 => "o",	2 => "/"	)
	);
		
	for ($cd1 = 0; $cd1 < 31; $cd1++)
	{
		for ($cd2 = 0; $cd2 < count($color_chardef[$color_def[$cd1]]); $cd2++)
		{
			$string = str_replace("^". $color_chardef[$color_def[$cd1]][$cd2], "</FONT><FONT COLOR=\"" . $color_def[$cd1] . "\">", $string);
		}
	}
	return $string;
}
?>
						<p>Last update: <?=time() - filemtime($serverlist_file) ?> seconds ago</p>
						<p style="font-size: 8px">Click on the headers to sort the list!</p>
						<table class="tablesorter" id="sl" border="0" style="width: 100%; text-align:center;background-color:rgba(255,255,255,0.3)">
							<thead><tr>
								<th>Server Name/IP</th>
								<th style="border-right: 0px; text-align: right">Current</th>
								<th style="border-left: 0px; border-right: 0px">/</th>
								<th style="border-left: 0px; width: 100px; text-align: left">max players</th>
								<th style="width: 200px">Map</th>
								<th>Gametype</th>
								<!--<th>Mod(s)</th>-->
								<th>Country</th>
							</tr></thead><tbody>
<? $odd = false; ?>
<? foreach(file($serverlist_file) as $line) { ?>
<? $line = trim($line); ?>
<? list($name,$ip,$players,$map,$type,$mod,$country) = explode("|!|!|", $line . "|!|!|"); ?>
<? $ixp = explode(":", $ip); ?>
<? if(in_array($ixp[0], file("data/servers.banlist")) || in_array($ip, file("data/servers.banlist"))) continue; ?>
<? $type = parse_gametype($type); ?>
<? if(isset($_MAPS["thumbs"][$map])) $map_thumb = $_MAPS["thumbs"][$map]; else $map_thumb = "/images/no_map.png"; ?>
<!-- map_thumb = <?=$map_thumb ?>; thumb count is <?=@count($_MAPS["thumbs"]) ?>; map = <?=$map ?> -->
<? if(!@empty($_MAPS["mw2"][$map])) $map = @$_MAPS["mw2"][$map]; ?>
<? list($players,$maxplayers) = explode("/", $players); ?>
<? if(strstr($map, "<") > -1) $map = "Someone tried to troll here but failed hard."; ?>
<? $country = "<span style=\"display:none\">$country</span><img alt=\"$country\" title=\"$country\" src=\"".parse_flag($country)."\" />"; ?>
							<tr style="height: 50px">
								<td style="text-align:left"><span style="display: none"><?=preg_replace("/[^a-zA-Z0-9\s]/", "", strip_tags(parse_gamecolors($name))); ?></span>
									<span style="font-size: 1.5em; font-family: 'Play', 'OCR A Extended'"><?=parse_gamecolors($name) ?></span><br />
									<span style="font-size: 1.3em"><?=$ip ?></span>
								</td>
								<td style="text-align: right; font-size: 1.5em"><?=$players ?></td>
								<td style="font-size: 1.5em">/</td>
								<td style="text-align: left; font-size: 1.5em; width: 50px"><?=$maxplayers ?></td>
								<td style="font-family: Electrolize; letter-spacing: 1px; font-size: 1.4em; text-align:right; display: color:white;background-size:100%;background-position:center center;background-image:url(<?=$map_thumb ?>)">
									<div class="blackgradient gradient"><?=$map ?></div>
								</td>
								<td><?=parse_gamecolors($type) ?></td>
								<!--<td><?=parse_gamecolors($mod) ?></td>-->
								<td><?=$country ?></td>
							</tr>
<? $odd = !$odd; ?>
<? } ?>
						</tbody></table>

						<script type="text/javascript">
							$(document).ready(function() 
							{ 
								$("#sl").tablesorter({sortList: [ [0,0] ]} );
/*
								setInterval(function() {
									$("#sl tr:odd").animate({
										"backgroundColor": "rgba(255,255,255,0.1)"
									}, 500);
									$("#sl tr:even").animate({
										"backgroundColor": "rgba(255,255,255,0)"
									}, 500);
								}, 1000);
*/
							}); 
						</script>
<? } ?>
			</div>
