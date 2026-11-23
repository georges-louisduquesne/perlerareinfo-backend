<?php
/**
 * Plugin Name: Perle Rare — bandeau des fiches conseiller
 * Description: Sur les pages individuelles de conseiller, masque le prénom et le nom du bandeau supérieur. Ce titre reprend le nom déjà affiché dans la fiche.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Fiche conseiller : le contenu porte le bloc nom-chasseur-immobilier-noir.
 * Les pages liste (nom-chasseur-immobilier, sans -noir) ne sont pas concernées.
 */
function perle_rare_is_fiche_conseiller()
{
    if (!is_singular('page')) {
        return false;
    }
    $post = get_post();
    if (!$post instanceof WP_Post) {
        return false;
    }
    return strpos((string) $post->post_content, 'nom-chasseur-immobilier-noir') !== false;
}

add_filter('body_class', static function ($classes) {
    if (perle_rare_is_fiche_conseiller()) {
        $classes[] = 'pr-fiche-conseiller';
    }
    return $classes;
});

add_action('wp_head', static function () {
    if (!perle_rare_is_fiche_conseiller()) {
        return;
    }
    echo "<style id=\"pr-fiche-banner\">body.pr-fiche-conseiller h1.perle-rare-head{visibility:hidden}</style>\n";
}, 99);
