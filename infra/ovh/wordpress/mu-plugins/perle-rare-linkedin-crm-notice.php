<?php
/**
 * Plugin Name: Perle Rare — fiches conseiller pilotées par le CRM
 * Description: Avertit, dans l’éditeur des pages qui affichent un conseiller, que ses données maîtresses sont publiées depuis perle-rare.info.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Texte de l'avertissement éditeur. La page personnelle rappelle que le titre
 * sous le nom (CP_Fonction ou CP_FonctionEn) est recopié depuis la fiche.
 */
function perle_rare_crm_master_notice($is_profile)
{
    if ($is_profile) {
        return '<strong>Prénom/nom, titre sous le nom, parcours, RSAC, e-mail, LinkedIn et fiche Google pilotés par perle-rare.info.</strong> Ces informations sont enregistrées sur la fiche conseiller. Une modification faite ici sera remplacée au prochain enregistrement de cette fiche.';
    }
    return '<strong>Prénom/nom, e-mail, LinkedIn et fiche Google des conseillers pilotés par perle-rare.info.</strong> Ils sont enregistrés sur chaque fiche conseiller. Une modification faite ici sera remplacée au prochain enregistrement de la fiche.';
}

add_action('admin_notices', static function () {
    if (!is_admin() || !function_exists('get_current_screen')) {
        return;
    }
    $screen = get_current_screen();
    if (!$screen || $screen->base !== 'post') {
        return;
    }
    $post_id = isset($_GET['post']) ? (int) $_GET['post'] : 0;
    if ($post_id <= 0) {
        return;
    }
    $post = get_post($post_id);
    if (!$post instanceof WP_Post || $post->post_type !== 'page') {
        return;
    }
    if (!current_user_can('edit_post', $post_id)) {
        return;
    }
    $content = (string) $post->post_content;
    if (strpos($content, 'nom-chasseur-immobilier') === false) {
        return;
    }
    $is_profile = strpos($content, 'nom-chasseur-immobilier-noir') !== false;
    $message = perle_rare_crm_master_notice($is_profile);
    echo '<div class="notice notice-warning"><p>' . $message . '</p></div>';
});
