<?php
/**
 * Plugin Name: Perle Rare — enveloppe des fiches
 * Description: Sur une fiche conseiller, l'adresse écrite à côté de LinkedIn ou Google redevient l'icône enveloppe.
 */

if (!defined('ABSPATH')) {
    exit;
}

add_filter('the_content', static function ($content) {
    if (!function_exists('perle_rare_is_fiche_conseiller') || !perle_rare_is_fiche_conseiller()) {
        return $content;
    }
    return perle_rare_fiche_envelope_icon($content);
}, 30);

/**
 * Ne touche pas une fiche qui a déjà l'enveloppe, ni une fiche dont le seul contact est l'adresse écrite.
 */
function perle_rare_fiche_envelope_icon($content)
{
    if (!is_string($content)) {
        return $content;
    }
    if (stripos($content, 'envelope-solid') !== false) {
        return $content;
    }
    if (stripos($content, 'linkedin-brands') === false && stripos($content, 'google-brands') === false) {
        return $content;
    }
    $icon = '<img class="alignnone size-full wp-image-4359" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png" alt="" width="30" height="30" />';
    $replaced = preg_replace_callback(
        '/<a\b[^>]*\bhref\s*=\s*(["\'])mailto:([^"\']+)\1[^>]*>\s*([^<]+)<\/a>/i',
        static function ($match) use ($icon) {
            $label = trim(html_entity_decode(wp_strip_all_tags($match[3]), ENT_QUOTES | ENT_HTML5, 'UTF-8'));
            if (!filter_var($label, FILTER_VALIDATE_EMAIL)) {
                return $match[0];
            }
            return '<a href="mailto:' . $match[2] . '">' . $icon . '</a>';
        },
        $content,
        1
    );
    return is_string($replaced) ? $replaced : $content;
}
