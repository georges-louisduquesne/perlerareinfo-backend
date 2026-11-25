<?php
/**
 * Bouton carte virtuelle et contacts anglais des fiches.
 * Usage : php tests/PerleRareFicheCarteTests.php
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

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-fiche-carte.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n$actual\n--- expected ---\n$expected\n");
    }
}

expect_same('french label', perle_rare_fiche_carte_label(false), 'Carte virtuelle');
expect_same('english label', perle_rare_fiche_carte_label(true), 'Virtual card');
expect_same(
    'card url stays on the site',
    perle_rare_fiche_carte_href('https://perle-rare.com/fr/carte-virtuelle-laurence-berry'),
    'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/'
);
expect_same('foreign card url is refused', perle_rare_fiche_carte_href('https://example.com/fr/carte-virtuelle-x/'), '');

$page = '<p><a class="lien-noir" href="mailto:lb@perle-rare.com">lb@perle-rare.com</a></p>';
$english = perle_rare_fiche_present_english_contacts(
    $page,
    'lb@perle-rare.com',
    'https://www.linkedin.com/in/laurence-berry/'
);
expect_same('english page shows the envelope', strpos($english, 'envelope-solid.png') !== false, true);
expect_same('english page hides the written address', strpos($english, '>lb@perle-rare.com</a>') === false, true);
expect_same('english page shows linkedin when the fiche has one', strpos($english, 'linkedin-brands.png') !== false, true);

$mailOnly = perle_rare_fiche_present_english_contacts($page, 'lb@perle-rare.com', '');
expect_same('missing linkedin stays absent', strpos($mailOnly, 'linkedin-brands') === false, true);
expect_same('envelope still replaces the address', strpos($mailOnly, 'envelope-solid.png') !== false, true);

$withButton = perle_rare_fiche_with_carte_button($english, 'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/', true);
expect_same('english button label', strpos($withButton, '>Virtual card</a>') !== false, true);
expect_same(
    'button is not duplicated',
    substr_count(perle_rare_fiche_with_carte_button($withButton, 'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/', true), 'pr-card-launch'),
    1
);

$french = '<p>Déjà publié.</p>';
$frenchButton = perle_rare_fiche_with_carte_button($french, 'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/', false);
expect_same('french button label', strpos($frenchButton, '>Carte virtuelle</a>') !== false, true);

$section = '<section class="section"><div class="container"><div class="row"><div class="col-md-6">'
    . '<div class="col-text-1"><h1 class="nom-chasseur-immobilier-noir"><strong>Claire-Charlotte FOITY</strong></h1>'
    . '<p>Bio.</p>'
    . '<p><a href="mailto:claire-charlotte.foity@perle-rare.com"><img src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png"></a></p>'
    . '</div></div></div></div></section>';
$placed = perle_rare_fiche_with_carte_button($section, 'https://perle-rare.com/fr/carte-virtuelle-claire-charlotte-foity/', false);
expect_same('button stays inside the fiche section', strpos($placed, 'pr-card-launch') < strpos($placed, '</section>'), true);
expect_same(
    'button follows the contact line',
    strpos($placed, 'envelope-solid-2.png"></a></p><p class="pr-card-launch">') !== false,
    true
);

$column = '<div class="col-text-1"><h1 class="nom-chasseur-immobilier-noir">Nom</h1><p>Sans contact.</p></div>';
$inColumn = perle_rare_fiche_with_carte_button($column, 'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/', false);
expect_same('button stays inside the text column', strpos($inColumn, 'pr-card-launch') < strpos($inColumn, '</div>'), true);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-fiche-carte tests passed\n";
