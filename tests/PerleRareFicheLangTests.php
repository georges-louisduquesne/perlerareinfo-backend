<?php
/**
 * Langue et fil d'Ariane des fiches conseiller anglaises.
 * Usage : php tests/PerleRareFicheLangTests.php
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

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-fiche-lang.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n" . var_export($actual, true) . "\n--- expected ---\n" . var_export($expected, true) . "\n");
    }
}

$schema = perle_rare_english_fiche_schema(
    'https://perle-rare.com/en/georges-louis-duquesne-director/',
    'Georges-Louis Duquesne – Director & Co-founder | Perle Rare',
    'Georges-Louis Duquesne'
);
$encoded = perle_rare_english_fiche_schema(
    'https://perle-rare.com/en/georges-louis-duquesne-director/',
    'Georges-Louis Duquesne – Director &amp; Co-founder | Perle Rare',
    'Georges-Louis Duquesne'
);
expect_same('title ampersand decoded', $encoded['@graph'][0]['name'], 'Georges-Louis Duquesne – Director & Co-founder | Perle Rare');
expect_same('page language', $schema['@graph'][0]['inLanguage'], 'en-GB');
expect_same('breadcrumb home name', $schema['@graph'][1]['itemListElement'][0]['name'], 'Home');
expect_same('breadcrumb home url', $schema['@graph'][1]['itemListElement'][0]['item'], 'https://perle-rare.com/en/');
expect_same('breadcrumb person', $schema['@graph'][1]['itemListElement'][1]['name'], 'Georges-Louis Duquesne');
$json = json_encode($schema);
expect_same('printed schema has no french home', strpos($json, 'Accueil') === false && strpos($json, '/fr/') === false, true);

$graph = array(
    array('@type' => 'WebPage', 'inLanguage' => 'fr-FR', 'url' => 'https://perle-rare.com/en/fabiola/'),
    array('@type' => 'ImageObject', 'inLanguage' => 'fr-FR'),
    array(
        '@type' => 'BreadcrumbList',
        'itemListElement' => array(
            array('name' => 'Accueil', 'item' => 'https://perle-rare.com/fr/'),
            array('name' => 'Fabiola Hemmet'),
        ),
    ),
    array('@type' => 'WebSite', 'inLanguage' => 'fr-FR'),
    array(
        '@type' => 'Organization',
        'logo' => array('inLanguage' => 'fr-FR', 'width' => 1),
    ),
);
$fixed = perle_rare_localize_english_fiche_graph($graph);
expect_same('webpage language', $fixed[0]['inLanguage'], 'en-GB');
expect_same('portrait language', $fixed[1]['inLanguage'], 'en-GB');
expect_same('breadcrumb rewritten', $fixed[2]['itemListElement'][0], array('name' => 'Home', 'item' => 'https://perle-rare.com/en/'));
expect_same('person kept', $fixed[2]['itemListElement'][1]['name'], 'Fabiola Hemmet');
expect_same('website stays french', $fixed[3]['inLanguage'], 'fr-FR');
expect_same('organization logo stays', $fixed[4]['logo']['inLanguage'], 'fr-FR');

expect_same('pair count', count(perle_rare_advisor_language_pairs()), 40);
$fabiola = perle_rare_advisor_language_pair('https://perle-rare.com/en/fabiola-hemmet-personal-consultant-perle-rare');
expect_same('fabiola french url', $fabiola[0], 'https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/');
expect_same('unknown page has no pair', perle_rare_advisor_language_pair('https://perle-rare.com/en/property-hunter-courbevoie/'), null);

$bare = '<html><head><link rel="canonical" href="https://perle-rare.com/en/fabiola-hemmet-personal-consultant-perle-rare/" /></head><body></body></html>';
$withLinks = perle_rare_insert_missing_hreflang($bare);
expect_same('missing hreflang is added', strpos($withLinks, 'hreflang="fr"') !== false && strpos($withLinks, 'hreflang="en"') !== false, true);
expect_same('already linked page is unchanged', perle_rare_insert_missing_hreflang($withLinks), $withLinks);
$other = '<html><head><link rel="canonical" href="https://perle-rare.com/en/property-hunter-courbevoie/" /></head></html>';
expect_same('non advisor page unchanged', perle_rare_insert_missing_hreflang($other), $other);

$identity = perle_rare_advisor_identity_from_content('<p class="nom-chasseur-immobilier-noir"><strong>Georges-Louis Duquesne</strong><br /> Dirigeant co-fondateur</p>');
expect_same('french name', $identity['name'], 'Georges-Louis Duquesne');
expect_same('french job', $identity['jobTitle'], 'Dirigeant co-fondateur');
$englishIdentity = perle_rare_advisor_identity_from_content("<h1 class=\"nom-chasseur-immobilier-noir\"><strong>Fabiola Hemmet</strong>\nPersonal consultant</h1>");
expect_same('english job', $englishIdentity['jobTitle'], 'Personal consultant');
$card = perle_rare_advisor_identity_from_content('<p class="nom-chasseur-immobilier"><strong>Jeanne</strong><br />Conseiller</p>');
expect_same('card is not a fiche', $card['name'], '');

$content = '<a href="mailto:a@perle-rare.com"><img src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png" /></a> <a href="https://www.linkedin.com/in/glduquesne/"><img src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png" /></a>';
expect_same('linkedin profile', perle_rare_first_linkedin_url($content), 'https://www.linkedin.com/in/glduquesne/');
expect_same('no linkedin', perle_rare_first_linkedin_url('<img src="linkedin-brands.png" />'), '');

$person = perle_rare_advisor_person(
    'https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/',
    'Georges-Louis Duquesne',
    'Dirigeant co-fondateur',
    'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-georges-louis-duquesne-1.jpg',
    'https://www.linkedin.com/in/glduquesne/',
    'fr-FR'
);
expect_same('person language', $person['inLanguage'], 'fr-FR');
expect_same('person employer', $person['worksFor']['name'], 'Perle Rare');
expect_same('person linkedin', $person['sameAs'][0], 'https://www.linkedin.com/in/glduquesne/');
$barePerson = perle_rare_advisor_person('https://perle-rare.com/en/marion-normand-2/', 'Marion Normand', '', '', '', 'en-GB');
expect_same('optional fields omitted', isset($barePerson['jobTitle']) || isset($barePerson['image']) || isset($barePerson['sameAs']), false);

$french = perle_rare_french_fiche_schema(
    'https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/',
    'Georges-Louis Duquesne – Dirigeant co-fondateur | Perle Rare',
    $person
);
expect_same('french page language', $french['@graph'][0]['inLanguage'], 'fr-FR');
expect_same('french page points at the person', $french['@graph'][0]['mainEntity']['@id'], $person['@id']);
expect_same('french schema has no english home', strpos(json_encode($french), 'https://perle-rare.com/en/') === false, true);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-fiche-lang tests passed\n";
