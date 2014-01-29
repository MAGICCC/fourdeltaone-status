function init_cache_request()
{
	ws_connect(); // Connect websocket
}

function init_content()
{
	$("#content").fadeIn("slow");
}

$(document).ready(init_cache_request);
$(document).ready(init_content);