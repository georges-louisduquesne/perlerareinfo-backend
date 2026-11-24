<?php
/**
 * Plugin Name: Perle Rare — langue des fiches anglaises
 * Description: Sur les fiches conseiller en anglais, le balisage déclare l'anglais et le fil d'Ariane pointe vers l'accueil anglais. Les pages françaises ne sont pas modifiées. Le hreflang réciproque est déjà émis par le site.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Fiche conseiller anglaise : le contenu porte nom-chasseur-immobilier-noir
 * et la langue courante est l'anglais. Les cartes et les pages liste n'ont pas ce bloc.
 */
function perle_rare_is_english_advisor_page()
{
    if (!function_exists('is_singular') || !is_singular('page')) {
        return false;
    }
    $post = get_post();
    if (!$post instanceof WP_Post) {
        return false;
    }
    if (strpos((string) $post->post_content, 'nom-chasseur-immobilier-noir') === false) {
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
function perle_rare_english_fiche_schema($url, $seoTitle, $personName)
{
    $url = (string) $url;
    return array(
        '@context' => 'https://schema.org',
        '@graph' => array(
            array(
                '@type' => 'WebPage',
                '@id' => $url,
                'url' => $url,
                'name' => (string) $seoTitle,
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
                        'name' => (string) $personName,
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
