<?php
/**
 * Plugin Name: Perle Rare — langue des fiches anglaises
 * Description: Sur les fiches conseiller en anglais, le balisage déclare l'anglais et le fil d'Ariane pointe vers l'accueil anglais. Ajoute le hreflang réciproque quand le site ne l'a pas déjà émis.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Fiche conseiller anglaise : le contenu porte nom-chasseur-immobilier-noir
 * et la langue courante est l'anglais. Les cartes et les pages liste n'ont pas ce bloc.
 */
function perle_rare_is_advisor_fiche()
{
    if (!function_exists('is_singular') || !is_singular('page')) {
        return false;
    }
    $post = get_post();
    if (!$post instanceof WP_Post) {
        return false;
    }
    return strpos((string) $post->post_content, 'nom-chasseur-immobilier-noir') !== false;
}

function perle_rare_is_english_advisor_page()
{
    if (!perle_rare_is_advisor_fiche()) {
        return false;
    }
    if (function_exists('pll_current_language')) {
        return pll_current_language('slug') === 'en';
    }
    $locale = function_exists('get_locale') ? (string) get_locale() : '';
    return strncmp($locale, 'en', 2) === 0;
}

/**
 * Balisage affiché sur la fiche anglaise : la page est en anglais,
 * le premier pas du fil d'Ariane est l'accueil anglais.
 */
function perle_rare_plain_text($value)
{
    return html_entity_decode((string) $value, ENT_QUOTES, 'UTF-8');
}

function perle_rare_english_fiche_schema($url, $seoTitle, $personName)
{
    $url = (string) $url;
    $seoTitle = perle_rare_plain_text($seoTitle);
    $personName = perle_rare_plain_text($personName);
    return array(
        '@context' => 'https://schema.org',
        '@graph' => array(
            array(
                '@type' => 'WebPage',
                '@id' => $url,
                'url' => $url,
                'name' => $seoTitle,
                'inLanguage' => 'en-GB',
                'breadcrumb' => array('@id' => $url . '#breadcrumb'),
            ),
            array(
                '@type' => 'BreadcrumbList',
                '@id' => $url . '#breadcrumb',
                'itemListElement' => array(
                    array(
                        '@type' => 'ListItem',
                        'position' => 1,
                        'name' => 'Home',
                        'item' => 'https://perle-rare.com/en/',
                    ),
                    array(
                        '@type' => 'ListItem',
                        'position' => 2,
                        'name' => $personName,
                    ),
                ),
            ),
        ),
    );
}

function perle_rare_schema_types($piece)
{
    if (!is_array($piece) || !isset($piece['@type'])) {
        return array();
    }
    return is_array($piece['@type']) ? $piece['@type'] : array($piece['@type']);
}

/**
 * Corrige le graphe Yoast d'une fiche anglaise sans toucher au site ni à l'organisation,
 * qui restent en français.
 */
function perle_rare_localize_english_fiche_graph($graph)
{
    if (!is_array($graph)) {
        return $graph;
    }
    foreach ($graph as $index => $piece) {
        if (!is_array($piece)) {
            continue;
        }
        $types = perle_rare_schema_types($piece);
        if (in_array('WebSite', $types, true) || in_array('Organization', $types, true)) {
            continue;
        }
        if (array_key_exists('inLanguage', $piece)) {
            $piece['inLanguage'] = 'en-GB';
        }
        if (in_array('BreadcrumbList', $types, true) && isset($piece['itemListElement'][0]) && is_array($piece['itemListElement'][0])) {
            $item = isset($piece['itemListElement'][0]['item']) ? $piece['itemListElement'][0]['item'] : '';
            $name = isset($piece['itemListElement'][0]['name']) ? $piece['itemListElement'][0]['name'] : '';
            if ($item === 'https://perle-rare.com/fr/' || $name === 'Accueil') {
                $piece['itemListElement'][0]['name'] = 'Home';
                $piece['itemListElement'][0]['item'] = 'https://perle-rare.com/en/';
            }
        }
        $graph[$index] = $piece;
    }
    return $graph;
}

function perle_rare_normalize_public_url($url)
{
    $url = strtolower(trim((string) $url));
    $url = preg_replace('#^http://#', 'https://', $url);
    return rtrim($url, '/');
}

/**
 * Couples de pages personnelles issus de conseillers_personnels (page web FR / EN).
 * Chaque URL est la clé de sa paire.
 */
function perle_rare_advisor_language_pairs()
{
    return array(
        array('https://perle-rare.com/fr/alexandre-gagnat-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/alexandre-gagnat-property-hunter-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/anne-coste-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/anne-coste-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/anne-davoli-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/anne-davoli-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/ariane-angeloglou-conseiller-personnel/', 'https://perle-rare.com/en/ariane-angeloglou-personal-consultant/'),
        array('https://perle-rare.com/fr/aude-chamboredon-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/aude-chamboredon-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/benedicte-peynet-conseiller-personnel/', 'https://perle-rare.com/en/benedicte-peynet-personal-consultant/'),
        array('https://perle-rare.com/fr/caroline-charvet-conseiller-personnel/', 'https://perle-rare.com/en/caroline-charvet-personal-consultant/'),
        array('https://perle-rare.com/fr/caroline-rapp-trevisan-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/caroline-rapp-trevisan-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/catherine-heuze-conseiller-personnel/', 'https://perle-rare.com/en/catherine-heuze-personal-consultant/'),
        array('https://perle-rare.com/fr/celine-thieriet-dirigeante-co-fondatrice/', 'https://perle-rare.com/en/celine-thieriet-director/'),
        array('https://perle-rare.com/fr/charles-champin-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/charles-champin-personal-consultant-perle-rare-paris/'),
        array('https://perle-rare.com/fr/chloe-poissonnet-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/chloe-poissonnet-property-hunter-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/chloris-perrotin-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/chloris-perrotin-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/clemence-kamienny-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/clemence-kamienny-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/constance-perrin-conseiller-personnel/', 'https://perle-rare.com/en/constance-perrin-personal-consultant/'),
        array('https://perle-rare.com/fr/corinne-lelarge-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/corinne-lelarge-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/dessislava-pavlova-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/dessislava-pavlova-property-hunter-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/diane-de-monteynard-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/diane-de-monteynard-personal-consultant-perle-rare-en/'),
        array('https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/fabiola-hemmet-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/', 'https://perle-rare.com/en/georges-louis-duquesne-director/'),
        array('https://perle-rare.com/fr/isabelle-chetochine/', 'https://perle-rare.com/en/isabelle-chetochine-personal-consultant/'),
        array('https://perle-rare.com/fr/karine-warman-conseiller-personnel/', 'https://perle-rare.com/en/karine-warman-personal-consultant/'),
        array('https://perle-rare.com/fr/laurence-berry-conseiller-personnel/', 'https://perle-rare.com/en/laurence-berry-personal-consultant/'),
        array('https://perle-rare.com/fr/laurence-pinaire/', 'https://perle-rare.com/en/laurence-pinaire-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/lionel-jamet/', 'https://perle-rare.com/en/lionel-jamet-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/margot-saidy-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/margot-saidy-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/marion-normand/', 'https://perle-rare.com/en/marion-normand-2/'),
        array('https://perle-rare.com/fr/pascale-saint-jean-conseiller-personnel/', 'https://perle-rare.com/en/pascale-saint-jean-personal-consultant/'),
        array('https://perle-rare.com/fr/conseiller-personnel-florence-roux-de-bezieux-perle-rare/', 'https://perle-rare.com/en/personal-consultant-florence-roux-de-bezieux-perle-rare/'),
        array('https://perle-rare.com/fr/elsa-pons/', 'https://perle-rare.com/en/personal-consultant-perle-rare-elsa-pons/'),
        array('https://perle-rare.com/fr/marta-clery-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/personal-consultant-perle-rare-marta-clery/'),
        array('https://perle-rare.com/fr/catherine-blanchard-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-catherine-blanchard-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/amelie-cailliez-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-paris-amelie-cailliez-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/claire-charlotte-foity-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-paris-claire-charlotte-foity-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/chasseur-immobilier-paris-isabelle-lerre-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-paris-isabelle-lerre-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/marie-bariller-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-paris-marie-bariller/'),
        array('https://perle-rare.com/fr/stephane-trichard-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/property-hunter-paris-stephane-trichard-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/roxane-bache-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/roxane-bache-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/solveig-de-fontgalland-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/solveig-de-fontgalland-personal-consultant-perle-rare/'),
        array('https://perle-rare.com/fr/sophie-mccaffry-conseiller-personnel-perle-rare/', 'https://perle-rare.com/en/sophie-mccaffry-personal-consultant-perle-rare/'),
    );
}

function perle_rare_advisor_language_pair($url)
{
    static $index = null;
    if ($index === null) {
        $index = array();
        foreach (perle_rare_advisor_language_pairs() as $pair) {
            $fr = perle_rare_normalize_public_url($pair[0]);
            $en = perle_rare_normalize_public_url($pair[1]);
            $index[$fr] = array($pair[0], $pair[1]);
            $index[$en] = array($pair[0], $pair[1]);
        }
    }
    $key = perle_rare_normalize_public_url($url);
    return isset($index[$key]) ? $index[$key] : null;
}

function perle_rare_advisor_hreflang_markup($url)
{
    $pair = perle_rare_advisor_language_pair($url);
    if ($pair === null) {
        return '';
    }
    return '<link rel="alternate" href="' . $pair[0] . '" hreflang="fr" />' . "\n"
        . '<link rel="alternate" href="' . $pair[1] . '" hreflang="en" />' . "\n";
}

/**
 * Ajoute les deux liens hreflang si la page n'en a pas déjà.
 * Une fiche déjà reliée par le site (Polylang) n'est pas dupliquée.
 */
function perle_rare_insert_missing_hreflang($html)
{
    if (!is_string($html)) {
        return $html;
    }
    if (strpos($html, 'hreflang="fr"') !== false && strpos($html, 'hreflang="en"') !== false) {
        return $html;
    }
    $canonical = '';
    if (preg_match('/<link[^>]*rel=["\']canonical["\'][^>]*href=["\']([^"\']+)["\']/', $html, $match)
        || preg_match('/<link[^>]*href=["\']([^"\']+)["\'][^>]*rel=["\']canonical["\']/', $html, $match)) {
        $canonical = html_entity_decode($match[1], ENT_QUOTES, 'UTF-8');
    }
    if ($canonical === '') {
        return $html;
    }
    $markup = perle_rare_advisor_hreflang_markup($canonical);
    if ($markup === '') {
        return $html;
    }
    $updated = preg_replace('/<head\b[^>]*>/', '$0' . "\n" . $markup, $html, 1);
    return is_string($updated) ? $updated : $html;
}

add_filter('wpseo_schema_graph', static function ($graph) {
    if (!perle_rare_is_english_advisor_page()) {
        return $graph;
    }
    return perle_rare_localize_english_fiche_graph($graph);
}, 20);

add_action('wp_head', static function () {
    if (!perle_rare_is_english_advisor_page()) {
        return;
    }
    $url = get_permalink();
    $seoTitle = function_exists('wp_get_document_title') ? wp_get_document_title() : get_the_title();
    $data = perle_rare_english_fiche_schema($url, $seoTitle, get_the_title());
    $flags = JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES;
    $json = function_exists('wp_json_encode') ? wp_json_encode($data, $flags) : json_encode($data, $flags);
    if (!is_string($json) || $json === '') {
        return;
    }
    echo "<script type=\"application/ld+json\" id=\"pr-fiche-lang\">" . $json . "</script>\n";
}, 20);

add_action('template_redirect', static function () {
    if (!perle_rare_is_advisor_fiche()) {
        return;
    }
    ob_start('perle_rare_insert_missing_hreflang');
}, 11);
