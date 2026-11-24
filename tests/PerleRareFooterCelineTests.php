<?php
/**
 * La phrase de contact avec Céline disparaît du pied de page, en français et en anglais.
 * Usage : php tests/PerleRareFooterCelineTests.php
 */

define('ABSPATH', __DIR__);

if (!function_exists('add_filter')) {
    function add_filter($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-footer-celine.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n" . var_export($actual, true) . "\n--- expected ---\n" . var_export($expected, true) . "\n");
    }
}

$french = '<p class="texte-footer">Venez nous rencontrer !<br /> Nous recrutons en permanence des chasseurs immobiliers à Paris.</p> '
    . '<p class="texte-footer">Prenez contact avec Céline (01 42 60 80 88 &#8211; <a href="https://perle-rare.com/nous-rejoindre/"><script type=text/javascript> var name = "ct" ; var domain = "perle-rare.com" ; document.write(name + \'@\' + domain) ;</script></a>) pour en savoir plus.</p>';
$english = '<p class="texte-footer">Pop in and meet us!<br /> We are always on the look-out for new property hunters in Paris.</p> '
    . '<p class="texte-footer">Get in touch with Céline (+33 (0)1 42 60 80 88 &#8211; <a href="https://perle-rare.com/join-us/">ct@perle-rare.com</a>) for more details.</p>';

$frenchHidden = perle_rare_footer_celine_hide($french);
expect_same('french sentence is gone', strpos($frenchHidden, 'Prenez contact avec') === false, true);
expect_same('french email script is gone', strpos($frenchHidden, 'document.write') === false, true);
expect_same('french recruitment stays', strpos($frenchHidden, 'Venez nous rencontrer') !== false, true);

$englishHidden = perle_rare_footer_celine_hide($english);
expect_same('english sentence is gone', strpos($englishHidden, 'Get in touch with') === false, true);
expect_same('english recruitment stays', strpos($englishHidden, 'Pop in and meet us') !== false, true);

$button = '<p class="pr-rejoindre-contact"><a class="pr-rejoindre-contact__btn" href="https://perle-rare.typeform.com/to/sbGYNJaj">Prenez contact ICI</a></p>';
expect_same('join button stays', perle_rare_footer_celine_hide($button), $button);
expect_same('second pass is stable', perle_rare_footer_celine_hide($frenchHidden), $frenchHidden);
expect_same('empty stays empty', perle_rare_footer_celine_hide(''), '');

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "ok\n";
