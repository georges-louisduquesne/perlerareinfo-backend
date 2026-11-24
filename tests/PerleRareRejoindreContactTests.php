<?php
/**
 * Bouton « Prenez contact ICI » en haut et en bas de la page Nous rejoindre.
 * Usage : php tests/PerleRareRejoindreContactTests.php
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

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-rejoindre-contact.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n" . var_export($actual, true) . "\n--- expected ---\n" . var_export($expected, true) . "\n");
    }
}

$phrase = '<p>Prenez contact <a href="https://perle-rare.typeform.com/to/sbGYNJaj"><strong>ICI</strong></a></p>';
$page = '<div class="element-bottom-40">' . $phrase . '</div><div class="element-bottom-80">' . $phrase . '</div>';

expect_same('page id', perle_rare_rejoindre_contact_page(856), true);
expect_same('other page stays out', perle_rare_rejoindre_contact_page(1), false);
expect_same('label', perle_rare_rejoindre_contact_label(), 'Prenez contact ICI');
expect_same('href', perle_rare_rejoindre_contact_href(), 'https://perle-rare.typeform.com/to/sbGYNJaj');

$once = perle_rare_rejoindre_contact_replace($page);
expect_same('both phrases become buttons', substr_count($once, $phrase), 0);
expect_same('two buttons', substr_count($once, 'pr-rejoindre-contact__btn'), 2);
expect_same('top block has a button', strpos($once, 'element-bottom-40') < strpos($once, 'pr-rejoindre-contact'), true);
expect_same('button label', substr_count($once, '>Prenez contact ICI</a>'), 2);
expect_same('same form', substr_count($once, 'https://perle-rare.typeform.com/to/sbGYNJaj'), 2);

$twice = perle_rare_rejoindre_contact_replace($once);
expect_same('second pass stays at two buttons', substr_count($twice, 'pr-rejoindre-contact__btn'), 2);
expect_same('second pass adds no phrase', substr_count($twice, $phrase), 0);

$other = perle_rare_rejoindre_contact_replace('<p>Prenez contact <a href="https://example.com/"><strong>ICI</strong></a></p>');
expect_same('other link stays', $other, '<p>Prenez contact <a href="https://example.com/"><strong>ICI</strong></a></p>');

$css = perle_rare_rejoindre_contact_css();
expect_same('width 150', strpos($css, 'width:150px') !== false, true);
expect_same('height 42', strpos($css, 'height:42px') !== false, true);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "ok\n";
