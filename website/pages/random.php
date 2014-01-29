<?php
$g = glob(ROOT . "/templates/givemesomethingtodo/*.html");
$g = $g[array_rand($g)];
include $g;