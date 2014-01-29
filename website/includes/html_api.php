<?

// No this is actually not an api. I don't care about that, too. I didn't find a better name for the file.

function page($id, $name, $url)
{
	return Array("id"=>$id, "title"=>$name, "url"=>$url);
}

function parse_attributes($attribs = Array())
{
	$o = "";
	foreach($attribs as $name => $value)
	{
		$o .= " $name=\"" . str_replace("\"", "\\\"", $value) . "\"";
	}
	return $o;
}

function menu()
{
	global $menu;
	extract($menu);
	require ROOT."/templates/menu.html";
}