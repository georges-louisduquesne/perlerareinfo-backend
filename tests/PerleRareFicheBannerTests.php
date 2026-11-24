<?php
/**
 * Transformations HTML du mu-plugin des fiches conseiller.
 * Usage : php tests/PerleRareFicheBannerTests.php
 */

define('ABSPATH', __DIR__);

if (!function_exists('add_filter')) {
    function add_filter($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}
if (!function_exists('add_action')) {
    function add_action($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-fiche-banner.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n$actual\n--- expected ---\n$expected\n");
    }
}

$fiche = <<<'HTML'
<header><h1 class="text-left perle-rare-head element-top-70 big bold">Georges-Louis Duquesne</h1></header>
<article>
<p class="nom-chasseur-immobilier-noir"><strong>Georges-Louis Duquesne</strong><br />
Dirigeant co-fondateur</p>
<p>Le parcours.</p>
</article>
HTML;

$once = perle_rare_render_fiche_headings($fiche);
$expected = <<<'HTML'
<header><p class="text-left perle-rare-head element-top-70 big bold">Georges-Louis Duquesne</p></header>
<article>
<h1 class="nom-chasseur-immobilier-noir"><strong>Georges-Louis Duquesne</strong><br />
Dirigeant co-fondateur</h1>
<p>Le parcours.</p>
</article>
HTML;
expect_same('fiche visible name becomes the only h1', $once, $expected);
expect_same('second pass is stable', perle_rare_render_fiche_headings($once), $expected);

$list = '<h1 class="perle-rare-head">L\'équipe</h1><p class="nom-chasseur-immobilier"><strong>Jeanne</strong></p>';
expect_same('list page name is not promoted', perle_rare_promote_visible_name_heading($list), $list);

$other = '<h1 class="entry-title">Autre</h1><p class="nom-chasseur-immobilier-noir"><strong>Diane</strong><br />Chasseur</p>';
expect_same(
    'another h1 stays',
    perle_rare_render_fiche_headings($other),
    '<h1 class="entry-title">Autre</h1><h1 class="nom-chasseur-immobilier-noir"><strong>Diane</strong><br />Chasseur</h1>'
);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-fiche-banner tests passed\n";
