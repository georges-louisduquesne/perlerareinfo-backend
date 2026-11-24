<?php
/**
 * Plugin Name: Perle Rare — note Google du pied de page
 * Description: Le badge Google du pied de page ouvre la fiche Google. Une fois par jour, la note est relue via l'API Places si PR_GOOGLE_PLACES_KEY est défini dans wp-config.php. Sans clé, la note affichée reste celle déjà enregistrée.
 */

if (!defined('ABSPATH')) {
    exit;
}

/** Fiche Google de Perle Rare, place Vendôme. */
function perle_rare_google_note_place_id()
{
    return 'ChIJKR8Q_S1u5kcRHTVJMJdpbKY';
}

/**
 * Lien vers la fiche. Une URL Google https déjà enregistrée est gardée.
 * Toute autre valeur retombe sur la fiche publique de la place Vendôme.
 */
function perle_rare_google_note_url($reviewUrl, $placeId)
{
    $id = is_string($placeId) && preg_match('/^ChIJ[A-Za-z0-9_-]+$/', $placeId)
        ? $placeId
        : perle_rare_google_note_place_id();
    $fallback = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+Chasseur+Immobilier&query_place_id=' . rawurlencode($id);
    if (!is_string($reviewUrl) || $reviewUrl === '') {
        return $fallback;
    }
    $parts = parse_url($reviewUrl);
    if (!is_array($parts) || empty($parts['scheme']) || empty($parts['host'])) {
        return $fallback;
    }
    if (strtolower($parts['scheme']) !== 'https') {
        return $fallback;
    }
    $host = strtolower($parts['host']);
    $allowed = array('google.com', 'www.google.com', 'maps.google.com', 'search.google.com');
    if (!in_array($host, $allowed, true)) {
        return $fallback;
    }
    return $reviewUrl;
}

/**
 * Enveloppe le badge Trustindex. Un second passage ne l'enveloppe pas deux fois.
 * Le reste du pied de page n'est pas modifié.
 */
function perle_rare_google_note_html($html, $url)
{
    if (!is_string($html) || strpos($html, 'ti-goog') === false) {
        return $html;
    }
    if (strpos($html, 'pr-google-note') !== false) {
        return $html;
    }
    $href = htmlspecialchars($url, ENT_QUOTES, 'UTF-8');
    return '<div class="pr-google-note"><a class="pr-google-note__link" href="'
        . $href
        . '" target="_blank" rel="noopener noreferrer">Avis Google de Perle Rare</a>'
        . $html
        . '</div>';
}

function perle_rare_google_note_css()
{
    return '.pr-google-note{position:relative;display:inline-block;max-width:100%}'
        . '.pr-google-note__link{position:absolute;inset:0;z-index:2;overflow:hidden;text-indent:-9999px}'
        . '.pr-google-note .ti-disclaimer,.pr-google-note .ti-info-icon{position:relative;z-index:3}';
}

/**
 * Réponse Places API (New) : rating + userRatingCount.
 * Une note hors de 1 à 5, ou un corps illisible, ne remplace rien.
 */
function perle_rare_google_rating_from_places_body($body)
{
    if (!is_string($body) || $body === '') {
        return null;
    }
    $data = json_decode($body, true);
    if (!is_array($data) || !isset($data['rating']) || !is_numeric($data['rating'])) {
        return null;
    }
    $score = (float) $data['rating'];
    if ($score < 1 || $score > 5) {
        return null;
    }
    $count = null;
    if (isset($data['userRatingCount']) && is_numeric($data['userRatingCount'])) {
        $parsed = (int) $data['userRatingCount'];
        if ($parsed >= 1) {
            $count = $parsed;
        }
    }
    return array(
        'score' => number_format($score, 1, '.', ''),
        'count' => $count,
    );
}

/**
 * Met à jour la note et le nombre d'avis. Le reste de la fiche (nom, adresse, URL) reste en place.
 */
function perle_rare_google_note_apply(array $details, array $rating)
{
    $details['rating_score'] = $rating['score'];
    if (isset($rating['count']) && $rating['count'] !== null) {
        $details['rating_number'] = (string) $rating['count'];
    }
    return $details;
}

function perle_rare_google_note_changed(array $before, array $after)
{
    $scoreBefore = isset($before['rating_score']) ? (string) $before['rating_score'] : '';
    $scoreAfter = isset($after['rating_score']) ? (string) $after['rating_score'] : '';
    $countBefore = isset($before['rating_number']) ? (string) $before['rating_number'] : '';
    $countAfter = isset($after['rating_number']) ? (string) $after['rating_number'] : '';
    return $scoreBefore !== $scoreAfter || $countBefore !== $countAfter;
}

add_filter('do_shortcode_tag', static function ($output, $tag) {
    if ($tag !== 'trustindex') {
        return $output;
    }
    $details = get_option('trustindex-google-page-details');
    $review = is_array($details) && isset($details['review_url']) ? $details['review_url'] : '';
    $place = is_array($details) && isset($details['id']) ? $details['id'] : '';
    return perle_rare_google_note_html($output, perle_rare_google_note_url($review, $place));
}, 10, 2);

add_action('wp_head', static function () {
    echo '<style id="pr-google-note">' . perle_rare_google_note_css() . "</style>\n";
}, 99);

add_action('init', static function () {
    if (!wp_next_scheduled('perle_rare_google_note_refresh')) {
        wp_schedule_event(time() + HOUR_IN_SECONDS, 'daily', 'perle_rare_google_note_refresh');
    }
});

add_action('perle_rare_google_note_refresh', 'perle_rare_google_note_refresh');

/**
 * Relit la note Google. Sans clé, ou si Google ne répond pas, la note affichée ne change pas.
 */
function perle_rare_google_note_refresh()
{
    if (!defined('PR_GOOGLE_PLACES_KEY') || !is_string(PR_GOOGLE_PLACES_KEY) || PR_GOOGLE_PLACES_KEY === '') {
        return;
    }
    $details = get_option('trustindex-google-page-details');
    if (!is_array($details)) {
        return;
    }
    $placeId = isset($details['id']) && is_string($details['id']) && preg_match('/^ChIJ[A-Za-z0-9_-]+$/', $details['id'])
        ? $details['id']
        : perle_rare_google_note_place_id();
    $response = wp_remote_get(
        'https://places.googleapis.com/v1/places/' . rawurlencode($placeId),
        array(
            'timeout' => 20,
            'headers' => array(
                'X-Goog-Api-Key' => PR_GOOGLE_PLACES_KEY,
                'X-Goog-FieldMask' => 'rating,userRatingCount',
            ),
        )
    );
    if (is_wp_error($response) || (int) wp_remote_retrieve_response_code($response) !== 200) {
        return;
    }
    $rating = perle_rare_google_rating_from_places_body(wp_remote_retrieve_body($response));
    if ($rating === null) {
        return;
    }
    $next = perle_rare_google_note_apply($details, $rating);
    update_option('perle_rare_google_note_checked', time(), false);
    if (!perle_rare_google_note_changed($details, $next)) {
        return;
    }
    update_option('trustindex-google-page-details', $next, false);
    if (function_exists('nitropack_sdk_purge')) {
        nitropack_sdk_purge(null, null, 'google-note');
    }
}
