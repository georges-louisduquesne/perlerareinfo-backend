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

$storedTwice = '<div class="col-text-1"><p><a href="mailto:gld@perle-rare.com"><img src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png"></a> '
    . '<a href="https://www.linkedin.com/in/glduquesne/"><img src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png"></a></p>'
    . '<p class="pr-card-launch"><a href="https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/">Carte virtuelle</a></p>'
    . '<p class="pr-card-launch"><a href="https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/">Carte virtuelle</a></p></div>';
$oneButton = perle_rare_fiche_with_carte_button(
    $storedTwice,
    'https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/',
    false
);
expect_same('a stored second button is removed', substr_count($oneButton, 'pr-card-launch'), 1);
expect_same('the first stored button stays', substr_count($oneButton, '>Carte virtuelle</a>'), 1);
expect_same('contact icons stay when the extra button goes', substr_count($oneButton, 'envelope-solid'), 1);
expect_same('the remaining button stays in the column', strpos($oneButton, 'pr-card-launch') < strpos($oneButton, '</div>'), true);

$themeAdded = $oneButton;
$themePos = strpos($themeAdded, '</p>');
$themeAdded = substr($themeAdded, 0, $themePos + 4)
    . '<p class="pr-card-launch"><a href="https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/">Carte virtuelle</a></p>'
    . substr($themeAdded, $themePos + 4);
expect_same(
    'a button added later by the theme is removed',
    substr_count(perle_rare_fiche_single_carte_button($themeAdded), 'pr-card-launch'),
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

$caroline = '<section class="section"><div class="col-text-1"><p>Bio.</p>'
    . '<p><a class="lien-noir" href="mailto:crt@perle-rare.com">crt@perle-rare.com</a></p></div></section>';
$carolineShown = perle_rare_fiche_present_english_contacts(
    $caroline,
    'caroline.rapptrevisan@perle-rare.com',
    'https://www.linkedin.com/in/caroline-rapp-tr%C3%A9visan-423211211/'
);
$carolineShown = perle_rare_fiche_with_carte_button(
    $carolineShown,
    'https://perle-rare.com/fr/carte-virtuelle-caroline-rapp-trevisan/',
    false
);
expect_same('french page uses CP_Mel', strpos($carolineShown, 'mailto:caroline.rapptrevisan@perle-rare.com') !== false, true);
expect_same('french page drops the wordpress alias', strpos($carolineShown, 'mailto:crt@perle-rare.com') === false, true);
expect_same('french page hides the written address', strpos($carolineShown, '>crt@perle-rare.com</a>') === false, true);
expect_same('french page shows the envelope inside the fiche', strpos($carolineShown, 'envelope-solid.png') < strpos($carolineShown, '</section>'), true);
expect_same('french page shows linkedin inside the fiche', strpos($carolineShown, 'linkedin-brands.png') < strpos($carolineShown, '</section>'), true);
expect_same('french page shows the card inside the fiche', strpos($carolineShown, 'pr-card-launch') < strpos($carolineShown, '</section>'), true);
expect_same('accented linkedin is kept', strpos($carolineShown, 'caroline-rapp-tr%C3%A9visan-423211211') !== false, true);

$complete = '<div class="col-text-1"><p><a href="mailto:claire-charlotte.foity@perle-rare.com">'
    . '<img src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png"></a> '
    . '<a href="https://www.linkedin.com/in/clairecharlottefoity/">'
    . '<img src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png"></a></p></div>';
$unchanged = perle_rare_fiche_present_english_contacts(
    $complete,
    'claire-charlotte.foity@perle-rare.com',
    'https://www.linkedin.com/in/clairecharlottefoity/'
);
expect_same('existing envelope is not duplicated', substr_count($unchanged, 'envelope-solid'), 1);
expect_same('existing linkedin is not duplicated', substr_count($unchanged, 'linkedin-brands'), 1);

$noLinkedin = perle_rare_fiche_present_english_contacts($caroline, 'caroline.rapptrevisan@perle-rare.com', '');
expect_same('missing linkedin stays absent on a french page', strpos($noLinkedin, 'linkedin-brands') === false, true);
expect_same('envelope still replaces the french address', strpos($noLinkedin, 'envelope-solid.png') !== false, true);
expect_same('envelope without linkedin still uses CP_Mel', strpos($noLinkedin, 'mailto:caroline.rapptrevisan@perle-rare.com') !== false, true);

$aliasIcon = '<p><a href="mailto:crt@perle-rare.com"><img src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png"></a></p>';
$aliasFixed = perle_rare_fiche_present_english_contacts($aliasIcon, 'caroline.rapptrevisan@perle-rare.com', '');
expect_same('existing envelope href becomes CP_Mel', strpos($aliasFixed, 'mailto:caroline.rapptrevisan@perle-rare.com') !== false, true);
expect_same('existing envelope drops the alias', strpos($aliasFixed, 'mailto:crt@') === false, true);
expect_same('existing envelope is not duplicated when the href changes', substr_count($aliasFixed, 'envelope-solid'), 1);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-fiche-carte tests passed\n";
