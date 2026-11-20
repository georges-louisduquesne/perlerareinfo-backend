-- Ajoute conseillers_personnels.CP_pageWebFr, CP_pageWebEn, CP_parcours_fr, CP_parcours_en
-- et alimente les 40 conseillers actifs (hors Candidat) depuis https://perle-rare.com (FR & EN).
-- Les colonnes sont en utf8mb4 / utf8mb4_unicode_ci pour préserver les apostrophes typographiques
-- et caractères français (la table historique étant en latin1).
-- Les 4 candidats actifs (CP_Statut = 'Candidat') et les conseillers inactifs restent à NULL.
--
-- Contrôle avant (2026-11-20, MariaDB 10.5.23) :
--   SELECT COUNT(*) FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo' AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME IN ('CP_pageWebFr', 'CP_pageWebEn', 'CP_parcours_fr', 'CP_parcours_en');
--   -- attendu : 0
--   SELECT COUNT(*) AS total_rows, SUM(CP_Actif = 1) AS active_rows,
--          SUM(CP_Actif = 1 AND CP_Statut <> 'Candidat') AS active_non_candidat
--     FROM conseillers_personnels;
--   -- attendu : 379 total, 44 actifs, 40 actifs hors Candidat
--
-- Retour arrière : voir 20261120-cp-pageweb-parcours-rollback.sql.

SET NAMES utf8mb4;

-- 1. Table de sauvegarde avant modification (ne pas supprimer dans la foulée).
CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261120_pageweb_parcours
  AS SELECT * FROM conseillers_personnels;

-- 2. Ajout des 4 colonnes NULLables.
ALTER TABLE conseillers_personnels
  ADD COLUMN IF NOT EXISTS CP_pageWebFr VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS CP_pageWebEn VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS CP_parcours_fr TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS CP_parcours_en TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL;

-- 3. Alimentation des 40 conseillers actifs présents sur perle-rare.com.
START TRANSACTION;

-- 0 : Georges-Louis Duquesne
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/',
       CP_pageWebEn = 'https://perle-rare.com/en/georges-louis-duquesne-director/',
       CP_parcours_fr = 'Georges-Louis possède une expérience de 20 années dans la banque et le financement immobilier des particuliers.

En appliquant des méthodes d’organisation éprouvées en entreprise, il permet aux Conseillers Perle Rare d’offrir à leurs clients un service moderne et fiable. Passionné par Paris, son histoire et ses quartiers, il vous communiquera son attachement pour la pierre. Il a co-fondé Perle Rare en octobre 2005.

Il est notamment en charge des finances, du suivi des missions de recherche et des fonctions support. Il est le garant du bon déroulement des missions de recherche.',
       CP_parcours_en = 'Georges-Louis has 20 years’ experience in the banking and personal mortgage sector.

By applying the organisational methods he learned and perfected in the financial industry, he ensures that Perle Rare consultants deliver a modern, reliable service. A great connoisseur of Paris, its history and its different neighbourhoods, he loves nothing more than to share his passion for his home city. Georges-Louis co-founded Perle Rare in October 2005.

He is in charge of our financial management, as well as keeping track of our search operations and support services. He makes sure that our assignments are a success.'
 WHERE CP_RefConseiller = 0 AND CP_Actif = 1;

-- 3 : Céline Thieriet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/celine-thieriet-dirigeante-co-fondatrice/',
       CP_pageWebEn = 'https://perle-rare.com/en/celine-thieriet-director/',
       CP_parcours_fr = 'Elle possède une expérience de plus de 15 années dans des entreprises de Services comme Directrice Marketing et Communication.

Elle a acquis une connaissance du secteur de l’immobilier dans un groupe international. Céline a vécu aux États-Unis et a été confrontée à la complexité des démarches liées à l’impatriation lors de son retour en France. Aujourd’hui, les quartiers du Marais, où elle a longtemps résidé, et du Luxembourg, sont ses lieux de prédilection dans la capitale et elle saura tout spécialement y dénicher votre Perle Rare.

Céline a co-fondée Perle Rare en octobre 2005. Elle est aujourd’hui en charge du commercial, du marketing et du recrutement de nouveaux talents. Elle est également le contact privilégié de la presse et des partenaires de Perle Rare.',
       CP_parcours_en = 'Céline has a wealth of marketing experience, having spent 15 years as a marketing and communications director in the service sector.

She learned the ropes of the property market while working for a large international firm. Céline spent some time living in the United States, and knows first-hand how complex it can be to find a property when moving back to France. At Perle Rare, she specialises in hunting out hidden gems in the Marais (her long-time home) and the area around the Jardin du Luxembourg.

Céline co-founded Perle Rare in October 2005. She handles our commercial affairs and marketing, as well as recruiting new talent. She is also the first point of call for press enquiries and our partners.'
 WHERE CP_RefConseiller = 3 AND CP_Actif = 1;

-- 55 : Ariane Angeloglou
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/ariane-angeloglou-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/ariane-angeloglou-personal-consultant/',
       CP_parcours_fr = 'Auteure du « Petit dictionnaire de l’amour, le pouvoir d’aimer » paru en 2007, de « A l’horizon de l’autre » en 2018 et comédienne de théâtre accomplie, Ariane a aussi l’atout d’avoir été directrice d’une agence immobilière.

Avec un sens de l’écoute avéré et une parfaite connaissance de Paris, elle a le souci de garantir en permanence l’adéquation entre les souhaits de ses clients et ce que le marché propose.

C’est un challenge permanent !',
       CP_parcours_en = 'Author of the “Little Dictionary of Love: the Power of Loving”, published in 2007, as well as an accomplished stage actress, Ariane’s busy career has also included a spell as director of an estate agency.

A great listener with an unrivalled knowledge of Paris, she works tirelessly to act as the bridge between our clients’ expectations and the realities of the market, which is a constantly-evolving challenge in itself !'
 WHERE CP_RefConseiller = 55 AND CP_Actif = 1;

-- 70 : Bénédicte Peynet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/benedicte-peynet-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/benedicte-peynet-personal-consultant/',
       CP_parcours_fr = 'Bénédicte est hyper dynamique, sportive accomplie et comédienne de théâtre amateur.

Spécialisée dans la relocation (installation de cadres étrangers et réinstallation de français à Paris) avant de rejoindre l’équipe de Perle Rare, elle saura vous faire bénéficier de sa riche expérience du marché parisien pour trouver l’appartement de vos rêves !',
       CP_parcours_en = 'Bénédicte is an accomplished sportswoman, a keen amateur actress and a blast of energy.

Before joining Perle Rare, Bénédicte specialised in professional relocation (moving international executives to Paris, and French professionals returning from overseas), giving her precious experience of the Parisian property market which she now uses to uncover our clients’ dream apartments !'
 WHERE CP_RefConseiller = 70 AND CP_Actif = 1;

-- 81 : Laurence Berry
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/laurence-berry-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/laurence-berry-personal-consultant/',
       CP_parcours_fr = 'Laurence a occupé des fonctions commerciales et marketing pendant de nombreuses années dans le monde de l’édition.

D’une joie de vivre et d’un enthousiasme à toute épreuve, elle sait lier des relations étroites avec ses clients pour mieux les comprendre et trouver le bien qui leur plaira !',
       CP_parcours_en = 'Before joining Perle Rare, Laurence spent several years working in commercial and marketing department in the publishing sector.

With her irrepressible enthusiasm and joie de vivre, Laurence is great at getting to know clients, understanding exactly what they want and finding the home of their dreams.'
 WHERE CP_RefConseiller = 81 AND CP_Actif = 1;

-- 93 : Karine Warman
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/karine-warman-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/karine-warman-personal-consultant/',
       CP_parcours_fr = 'Juriste de formation et mélomane, Karine a consacré une douzaine d’années à la production et à la diffusion de concerts.
On retrouve régulièrement cette passionnée de voile sur les pontons et inscrite pour une régate.
Sa sensibilité artistique, son sens du détail comme son goût du challenge seront des atouts précieux pour dénicher votre Perle Rare.',
       CP_parcours_en = 'A law graduate and music lover, Karine has devoted over a decade to producing and broadcasting concerts.

You can often find this sailing fan on the pontoons and entering regattas.

Her artistic sensibility, eye for detail and love for a challenge will be valuable assets to help you find your ‘perle rare’.'
 WHERE CP_RefConseiller = 93 AND CP_Actif = 1;

-- 102 : Amélie Cailliez
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/amelie-cailliez-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-paris-amelie-cailliez-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Amélie est une jeune femme extrêmement dynamique, dotée d’un sens inné et aigu de la créativité.
Créatrice de concepts originaux dans une autre vie , elle saura vous faire profiter de son imagination et de son énergie pour vous dégoter votre Perle Rare !',
       CP_parcours_en = 'Amélie is an extremely dynamic woman with an innate and keen sense of creativity.

Creative of original concepts in another life, she will know how to make you benefit from her imagination and her energy to find your Perle Rare!'
 WHERE CP_RefConseiller = 102 AND CP_Actif = 1;

-- 115 : Catherine Heuzé
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/catherine-heuze-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/catherine-heuze-personal-consultant/',
       CP_parcours_fr = 'Catherine est une femme dynamique et réfléchie, ancienne journaliste, réalisatrice dans l’audiovisuel et rédactrice en chef d’un magazine dédié à l’art de vivre, au design, à l’architecture et à la création.

Aujourd’hui, forte de cette expérience riche, Catherine saura vous guider, vous conseiller et vous accompagner dans la recherche de votre Perle Rare !',
       CP_parcours_en = 'Catherine is a dynamic and thoughtful woman, former journalist, audiovisual director and managing editor of a magazine dedicated to the art of living, design, architecture and creation.

Today, with this rich experience, Catherine will guide you, advise you and accompany you in the search for your Perle Rare!'
 WHERE CP_RefConseiller = 115 AND CP_Actif = 1;

-- 117 : Pascale Saint-Jean
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/pascale-saint-jean-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/pascale-saint-jean-personal-consultant/',
       CP_parcours_fr = 'Décoratrice dans le cinéma puis chez des particuliers, c’est plus récemment dans un Musée de France de la Capitale que Pascale a pu exprimer son amour des arts et de la création en tant que Chargée de Collection.

Parisienne de naissance, Pascale a vécu dans plusieurs arrondissements de la capitale ce qui lui confère une connaissance « intime » de la ville.

Attentive et généreuse, elle saura vous accompagner activement dans la recherche « sur-mesure » de votre Perle Rare.',
       CP_parcours_en = 'Decorator in the cinema industry and then in private homes, it was more recently in a french museum in Paris that Pascale was able to express her love of arts and creation as a Collection Manager.

Parisian by birth, Pascale has lived in several districts of the capital, which gives her an “intimate” knowledge of the city.

Attentive and generous, she will know how to actively support you in the “tailor-made” search for your Perle Rare.'
 WHERE CP_RefConseiller = 117 AND CP_Actif = 1;

-- 127 : Constance Perrin
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/constance-perrin-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/constance-perrin-personal-consultant/',
       CP_parcours_fr = 'Constance est une femme ultra dynamique, passionnée de décoration, domaine dans lequel elle a travaillé pendant 15 ans. Elle a un don pour imaginer la transformation des lieux.
Depuis quelques années Constance travaille dans l’immobilier, en passant par la relocation. Constance saura donc vous faire profiter de son expérience dans votre recherche de la perle rare !',
       CP_parcours_en = 'Constance is an ultra dynamic woman, passionate about decoration, a field in which she worked for 15 years. She has a gift for imagining the transformation of places.

For several years Constance has been working in real estate, including relocation. Constance will therefore be able to share with you her experience in your search for that special Perle Rare!'
 WHERE CP_RefConseiller = 127 AND CP_Actif = 1;

-- 128 : Caroline Charvet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/caroline-charvet-conseiller-personnel/',
       CP_pageWebEn = 'https://perle-rare.com/en/caroline-charvet-personal-consultant/',
       CP_parcours_fr = 'Caroline est une femme dynamique, disponible, rapide et efficace. Ancienne créatrice d’entreprise, elle aime être sur le terrain pour vous dénicher le bien de vos rêves, vous accompagner et vous conseiller au quotidien dans votre quête de la perle rare !',
       CP_parcours_en = 'Caroline is a dynamic, available, quick and efficient woman. As a former business founder she enjoys being in the field to find you the property of your dreams, to support and advise you on a daily basis in your quest for that Perle Rare!'
 WHERE CP_RefConseiller = 128 AND CP_Actif = 1;

-- 140 : Isabelle Lerre
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/chasseur-immobilier-paris-isabelle-lerre-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-paris-isabelle-lerre-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Isabelle est une femme ultra dynamique qui connait toutes les arcanes de l’immobilier.

Elle a travaillé aussi bien dans des grands groupes qu’à son propre compte, de la promotion immobilière à l’agence immobilière.

Aujourd’hui, forte de cette expérience, de sa connaissance du marché et de son envie d’accompagner ses clients au quotidien dans leur cheminement, elle saura vous écouter pour vous dénicher votre Perle Rare !',
       CP_parcours_en = 'Isabelle is an ultra-dynamic woman who knows all the secrets of real estate.

She has worked as well in large groups and on her own behalf, from real estate development to real estate agency.

Today, with this expertise, her knowledge of the market and her desire to accompany customers on a daily basis in their personal development, she will listen to you to find your Perle Rare!'
 WHERE CP_RefConseiller = 140 AND CP_Actif = 1;

-- 162 : Stéphane Trichard
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/stephane-trichard-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-paris-stephane-trichard-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Notre journaliste réalisateur !

Stéphane baigne dans l’immobilier et la recherche d’appartement depuis de nombreuses années puisqu’il réalisait depuis 2005 tous les sujets de Stéphane Plaza dans l’émission « Recherche appartement ou maison ».

Habitué au dynamisme, au stress et aux imprévus de dernières minutes générés par un tournage mais aussi passionné par Paris et son architecture, Stéphane saura vous prendre par la main, vous conseiller et vous trouver votre Perle Rare !',
       CP_parcours_en = 'Our journalist and director !

Stéphane works  in real estate and property search for many years. He had made all the subjects of Stéphane Plaza since 2005 for the program ” Recherche appartement ou maison “.

Accustomed to dynamism, stress and the unexpected event generated by a shooting. Also passionate about Paris and its architecture. Stéphane will be able to take you by the hand, advise you and find your perle rare!'
 WHERE CP_RefConseiller = 162 AND CP_Actif = 1;

-- 164 : Catherine Blanchard
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/catherine-blanchard-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-catherine-blanchard-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Notre surdiplômée de l’équipe !

Catherine est une ancienne spécialiste de la conduite du changement pour des grands groupes. Elle connait donc toutes les implications que comprennent les changements de lieu de vie.

Également dotée d’une grande patience et d’une grande douceur, elle saura vous guider dans la réussite de votre projet de recherche d’appartement, pour mieux trouver votre Perle Rare !',
       CP_parcours_en = 'Our overqualified women !

Catherine is a former specialist in change management within corporate groups. She therefore knows all the implications that include changes in living environments.

She also has a great patience and great sweetness, she will be able to guide you in the success of your apartment search project and find your perle rare !'
 WHERE CP_RefConseiller = 164 AND CP_Actif = 1;

-- 167 : Marie Bariller
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/marie-bariller-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-paris-marie-bariller/',
       CP_parcours_fr = 'Marie a exercé plusieurs métiers et a vécu à New York. Ancien mannequin, directrice de collection, directrice artistique mais aussi auteure de livres sur la mode et la décoration, également sophrologue, Marie est aussi passionnée par l’immobilier dans lequel elle investit depuis longtemps.

Ces différentes expériences ont développé son sens de l’esthétisme et du détail, son aptitude à écouter les besoins des clients. Elle saura mettre toutes ces qualités à votre service pour vous trouver la perle rare !',
       CP_parcours_en = 'Marie held various jobs and lived in New York.

Former model, executive collection, artistic director – author of books on fashion and decoration – sophrologist – Marie is also passionate about real estate in which she has been investing for a long time.

These different experiences have developed its sense of aesthetics and detail, its ability to listen to customer needs. She will be able to put all these qualities at your service to find your perle rare!'
 WHERE CP_RefConseiller = 167 AND CP_Actif = 1;

-- 168 : Claire-Charlotte Foity
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/claire-charlotte-foity-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/property-hunter-paris-claire-charlotte-foity-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Claire-Charlotte parle à l’oreille des chevaux ! Passionnée d’équitation, elle a un sens de l’écoute et de l’empathie très développé.

Elle a vécu les 20 premières années de sa vie à l’étranger, notamment en Asie. Elle a donc l’habitude du changement, des cultures différentes et du sens du service.

Ce mélange associé à sa connaissance de l’immobilier parisien font qu’elle saura vous écouter et vous guider afin de trouver votre perle rare !',
       CP_parcours_en = 'Claire-Charlotte is a horse whisperer! Passionate about horse riding, she has a very developed sense of listening and empathy.

She lived the first 20 years of her life abroad, particularly in Asia. She is therefore used to change, to different cultures and has a great sense of service.

This mix combined with her knowledge of Parisian real estate means that she will be able to listen to you and guide you in order to find your perle rare!'
 WHERE CP_RefConseiller = 168 AND CP_Actif = 1;

-- 171 : Margot Saidy
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/margot-saidy-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/margot-saidy-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Notre femme de l’orient ! Margot est une belle personnalité du levant, avec son humour spécifique.

Diplômée en psychologie, Margot a occupé plusieurs postes dans les Ressources Humaines.

Aimant les challenges, persévérante, elle saura trouver des solutions pour vous dénicher le bien de vos rêves, votre perle rare !',
       CP_parcours_en = 'Our woman from the middle-east! Margot is a beautiful personality from the Levant, with her specific humor.

As a graduated in psychology, Margot has held several positions in Human Resources.

Liking challenges, persevering, she will be able to find solutions to find you the property of your dreams, your perle rare!'
 WHERE CP_RefConseiller = 171 AND CP_Actif = 1;

-- 178 : Alexandre Gagnat
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/alexandre-gagnat-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/alexandre-gagnat-property-hunter-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Alexandre a géré sa propre agence immobilière pendant plus de 25 ans. Une partie de l’équipe Perle Rare travaillait avec lui régulièrement et appréciait son professionnalisme et son sens du relationnel.

C’est pourquoi, quand Alexandre a souhaité changer de métier pour nous rejoindre, c’est naturellement que nous l’avons intégré à l’équipe.

Grâce à son expertise immobilière, son dynamisme et son sens de l’écoute, Alexandre saura vous trouver votre perle rare !',
       CP_parcours_en = 'Alexandre managed his own real estate agency for over 25 years. Part of the Perle Rare team worked with him regularly and appreciated his professionalism and his interpersonal skills.

This is why, when Alexandre wanted to change careers to join us, we naturally included him in the team.

Thanks to his real estate expertise, his dynamism and his sense of listening, Alexandre will be able to find your perle rare!'
 WHERE CP_RefConseiller = 178 AND CP_Actif = 1;

-- 191 : Charles Champin
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/charles-champin-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/charles-champin-personal-consultant-perle-rare-paris/',
       CP_parcours_fr = 'Charles est notre couteau suisse.

Grâce à son expertise dans l’hôtellerie internationale et l’immobilier haut de gamme, Charles vous donne accès à une sélection exclusive d’actifs : hôtels, résidences hotelières, terrains viabilisés, espaces de coworking et coliving, hôtels particuliers et immeubles, principalement à Paris mais aussi en France et en Europe.

Charles vous fera bénéficier de son expertise, de sa réactivité pour bien cerner et évaluer votre projet afin de pouvoir le mener à bien.',
       CP_parcours_en = 'Charles is our Swiss Army knife.

Thanks to his expertise in international hospitality and high-end real estate, Charles gives you access to an exclusive selection of assets: hotels, serviced residences, ready-to-build land, coworking and coliving spaces, private mansions, and buildings — primarily in Paris, but also throughout France and Europe.

Charles will leverage his expertise and responsiveness to accurately understand and assess your project in order to successfully bring it to fruition.'
 WHERE CP_RefConseiller = 191 AND CP_Actif = 1;

-- 193 : Caroline Rapp Trévisan
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/caroline-rapp-trevisan-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/caroline-rapp-trevisan-consultant-perle-rare/',
       CP_parcours_fr = 'Ancienne militaire et conseillère en transaction immobilière, Caroline a su conjuguer rigueur et passion pour se réinventer dans l’immobilier.

Exploratrice dans l’âme, elle arpente les rues parisiennes avec une curiosité inépuisable pour dénicher des biens d’exception.

Sa discipline acquise dans l’armée, son expertise dans l’immobilier et son enthousiasme communicatif sont autant d’atouts pour vous accompagner dans la recherche de votre perle rare.',
       CP_parcours_en = 'Formerly a military officer and now a real estate consultant, Caroline has successfully combined discipline and passion to reinvent herself in the real estate world.

An explorer at heart, she roams the streets of Paris with endless curiosity, uncovering exceptional properties.

Her military-acquired discipline, expertise in real estate, and contagious enthusiasm are all invaluable assets to guide you in finding your perle rare.'
 WHERE CP_RefConseiller = 193 AND CP_Actif = 1;

-- 194 : Marion Normand
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/marion-normand/',
       CP_pageWebEn = 'https://perle-rare.com/en/marion-normand-2/',
       CP_parcours_fr = 'Marion est une experte reconnue en gestion des opérations et optimisation de la qualité de service.
Avec un rôle stratégique aux côtés des deux co-fondateurs, Céline Thieriet et Georges-Louis Duquesne, Marion travaille également en étroite collaboration avec l’équipe de 25 conseillers personnels pour consolider la position de Perle Rare en tant que leader de la chasse immobilière à Paris.
Dotée d’une solide expérience en gestion des ressources humaines et en chasse immobilière, Marion a pour mission d’affiner et de structurer nos processus internes, en mettant l’accent sur l’excellence des services proposés à nos clients. Grâce à sa vision axée sur l’amélioration continue, elle renforce non seulement la qualité des prestations, mais également la cohésion et l’efficacité des équipes commerciales.
Son expertise et son sens de l’innovation contribuent à établir des standards de service encore plus élevés, accélérant ainsi la performance globale de Perle Rare et confortant son leadership sur le marché parisien.',
       CP_parcours_en = 'Marion is a recognized expert in operations management and service quality optimization.
In her strategic role alongside the two co-founders, Céline Thieriet and Georges-Louis Duquesne, Marion will also work closely with the team of 25 consultants to further solidify Perle Rare’s position as a leader in property hunting in Paris.
With a strong background in human resources management and property scouting, Marion will be tasked with refining and structuring our internal processes, focusing on delivering excellence in the services offered to our clients.
Her vision, centered on continuous improvement, will enhance not only the quality of services but also the cohesion and efficiency of the sales teams.
Her expertise and innovative mindset will contribute to setting even higher service standards, accelerating Perle Rare’s overall performance and reinforcing its leadership in the Paris market.'
 WHERE CP_RefConseiller = 194 AND CP_Actif = 1;

-- 231 : Fabiola Hemmet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/fabiola-hemmet-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Avant de rejoindre Perle Rare, Fabiola a travaillé dans la communication et comme voix off professionnelle. Ce parcours l’a dotée d’une belle sensibilité et d’un excellent relationnel, qu’elle met aujourd’hui au service de vos projets immobiliers.

Fabiola accompagne ses clients avec attention et exigence dans leur recherche immobilière à Paris.Trilingue (français, anglais, espagnol), elle sait comprendre les attentes d’une clientèle française et internationale.

Avec une écoute fine, une grande réactivité et un vrai sens du détail, elle identifie les biens qui font la différence — même lorsqu’ils ne sont pas sur le marché.

Elle aime créer un lien de confiance durable avec chaque client et rendre l’expérience de recherche aussi fluide que possible.',
       CP_parcours_en = 'Before joining Perle Rare, Fabiola worked in communications and as a professional voice-over artist.
This background gave her strong interpersonal skills and a refined sensitivity, which she now brings to every real estate project.

Fabiola supports her clients with care and high standards in their property searches across Paris.
Trilingual in French, English, and Spanish, she understands the needs of both French and international clients.

Attentive, responsive, and detail-oriented, she finds properties that truly stand out — even those that aren’t officially on the market.
She values building lasting relationships with her clients and making the search experience as smooth and efficient as possible.'
 WHERE CP_RefConseiller = 231 AND CP_Actif = 1;

-- 239 : Corinne Noëlle Lelarge
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/corinne-lelarge-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/corinne-lelarge-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Dotée d’une solide expérience dans l’immobilier, Corinne excelle dans l’art de comprendre les attentes les plus exigeantes. Son écoute active, sa réactivité et son sens du service font d’elle une alliée précieuse pour concrétiser des projets immobiliers uniques, qu’il s’agisse d’une résidence principale, d’un pied-à-terre ou d’un investissement patrimonial.

Ce qui distingue Corinne ? Un œil affûté pour les lieux rares et un plaisir sincère à accompagner ses clients comme s’il s’agissait de proches. Passionnée par l’art de vivre à la parisienne, elle aime partager ses bonnes adresses, ses coins secrets et ses conseils avisés pour trouver votre perle rare !',
       CP_parcours_en = 'With a strong background in real estate, Corinne excels at understanding even the most discerning expectations. Her attentive listening, responsiveness, and commitment to service make her a valuable ally in bringing unique real estate projects to life—whether it’s a primary residence, a pied-à-terre, or a long-term investment.

What sets Corinne apart? A sharp eye for hidden gems and a genuine pleasure in guiding her clients as if they were close friends. Passionate about the Parisian art of living, she enjoys sharing her favorite spots, hidden corners, and expert tips to help you find your very own perle rare!'
 WHERE CP_RefConseiller = 239 AND CP_Actif = 1;

-- 256 : Aude Chamboredon
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/aude-chamboredon-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/aude-chamboredon-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Avec un parcours mêlant gestion de patrimoine et immobilier haut de gamme, Aude apporte à ses clients une expertise aussi solide que sensible. Fiable et profondément investie, elle incarne cette discrétion rassurante qui inspire immédiatement confiance. Chaque projet est pour elle une mission, qu’elle mène avec rigueur, bienveillance et une attention sincère aux attentes de ses clients.

Dotée d’un vrai sens du service et d’une connaissance fine du marché parisien, elle sait allier efficacité et élégance dans ses recherches. À l’écoute, intuitive et persévérante, Aude aime découvrir l’histoire derrière chaque projet de vie, pour proposer des biens qui ne ressemblent à aucun autre — sauf à celui que ses clients attendaient, leur perle rare !',
       CP_parcours_en = 'With a background combining wealth management and high-end real estate, Aude offers her clients an expertise that is both solid and thoughtful. Reliable and deeply committed, she embodies a reassuring discretion that immediately inspires trust. Every project is a mission she undertakes with precision, care, and a genuine attentiveness to her clients’ expectations.

Thanks to her strong sense of service and deep understanding of the Parisian market, she masterfully balances efficiency and elegance in her searches. Attentive, intuitive, and persistent, Aude enjoys uncovering the story behind each life project, to propose properties like no other—except the one her clients had been waiting for: their perle rare!'
 WHERE CP_RefConseiller = 256 AND CP_Actif = 1;

-- 265 : Anne Davoli
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/anne-davoli-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/anne-davoli-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Avant de rejoindre Perle Rare, Anne a exercé comme architecte d’intérieur et directrice artistique dans la mode. Ces expériences ont renforcé son sens de l’espace, du détail et de l’esthétique – des atouts précieux pour accompagner ses clients exigeants dans leur recherche d’un bien à Paris.

Anne accompagne avec passion ses clients dans leur projet immobilier, avec une attention particulière aux biens de charme et de caractère. Elle mobilise sa fine connaissance du marché parisien et son goût pour l’architecture pour dénicher des lieux uniques.

Elle construit avec eux une relation fondée sur l’écoute, la confiance et la transparence, pour les guider efficacement à chaque étape de leur projet.',
       CP_parcours_en = 'Before joining Perle Rare, Anne worked as an interior architect and artistic director in the fashion industry.
These experiences sharpened her sense of space, detail, and aesthetics — valuable assets when assisting discerning clients in their search for a property in Paris.

Anne supports her clients with passion in their real estate projects, with a special focus on properties full of charm and character.
She draws on her deep knowledge of the Paris market and her love of architecture to uncover truly unique places.

She builds strong relationships based on listening, trust, and transparency, guiding her clients through every step of their journey.'
 WHERE CP_RefConseiller = 265 AND CP_Actif = 1;

-- 274 : Sophie McCaffry
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/sophie-mccaffry-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/sophie-mccaffry-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Britannique d’origine et Française d’adoption, Sophie a posé ses valises à Paris il y a plus de trente ans. Ancienne journaliste culturelle, elle a cultivé un regard affûté sur la beauté, le détail et la singularité — autant de qualités qu’elle met aujourd’hui au service de l’immobilier.

Sa passion pour l’architecture parisienne et les biens de caractère l’a naturellement conduite vers la chasse immobilière, où elle retrouve ce qu’elle aime le plus : découvrir, raconter, et relier les gens aux lieux qui leur ressemblent.

Chez Perle Rare, Sophie met à profit son sens de l’écoute, sa curiosité et sa parfaite connaissance de Paris pour accompagner des clients exigeants dans la recherche de leur futur lieu de vie. Son regard sensible et international apporte une touche d’élégance supplémentaire à notre équipe.',
       CP_parcours_en = 'British by origin and French by adoption, Sophie settled in Paris more than thirty years ago.
A former cultural journalist, she has developed a keen eye for beauty, detail, and uniqueness — qualities she now brings to the world of real estate.

Her passion for Parisian architecture and characterful homes naturally led her to property hunting, where she finds what she loves most: discovering, storytelling, and connecting people with places that reflect who they are.

At Perle Rare, Sophie draws on her attentiveness, curiosity, and deep knowledge of Paris to guide discerning clients in their search for their next home.
Her sensitive and international outlook adds an extra touch of elegance to our team.'
 WHERE CP_RefConseiller = 274 AND CP_Actif = 1;

-- 276 : Chloris Perrotin
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/chloris-perrotin-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/chloris-perrotin-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Forte d’une expérience préalable dans la chasse immobilière, Chloris possède déjà les réflexes essentiels de ce métier : une compréhension rapide des projets, une gestion fluide des clients et une efficacité dans la conduite des recherches.

Passionnée par l’immobilier et dotée d’un regard sensible, elle allie rigueur et intuition pour transformer vos critères en un véritable projet de vie. Son sens de l’écoute et sa capacité à cerner les attentes lui permettent d’identifier rapidement les biens qui feront la différence.

Afin de trouver votre Perle Rare, Chloris met à profit cette expérience et cette énergie pour vous accompagner à chaque étape de votre acquisition, avec pour objectif de vous offrir un parcours sur mesure, fluide et exigeant.',
       CP_parcours_en = 'With prior experience in property hunting, Chloris already masters the essential skills of the profession: quickly understanding projects, managing client relationships with ease, and carrying out searches efficiently.

Passionate about real estate and gifted with a keen eye, she combines rigor and intuition to turn your criteria into a true life project. Her attentiveness and ability to understand expectations enable her to quickly identify the properties that truly stand out.

To help you find your very own Perle Rare, Chloris brings her experience and energy to support you at every stage of your acquisition, with the aim of providing a tailor-made, seamless, and high-quality journey.'
 WHERE CP_RefConseiller = 276 AND CP_Actif = 1;

-- 278 : Anne Coste
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/anne-coste-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/anne-coste-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Artiste accomplie et membre du duo musical Les Frangines, Anne a toujours eu le goût des belles histoires et des émotions vraies. C’est ce même sens de l’harmonie, de l’écoute et de l’intuition qui la guide aujourd’hui dans l’immobilier.

Passionnée depuis longtemps par l’art de se sentir bien quelque part, Anne retrouve dans la chasse immobilière ce qu’elle aimait déjà dans la musique : la recherche du bon accord. Comprendre une personne, capter ses envies, trouver la juste note entre rêve et réalité — voilà ce qui l’anime.

Chez Perle Rare, Anne met sa sensibilité, son énergie et son regard singulier au service de ceux qui cherchent un lieu à leur image. Parce qu’au fond, dénicher un bien, c’est aussi composer une nouvelle mélodie de vie.',
       CP_parcours_en = 'An accomplished artist and member of the musical duo Les Frangines, Anne has always had a passion for beautiful stories and genuine emotions.
That same sense of harmony, attentiveness, and intuition now guides her in the world of real estate.

Long fascinated by the art of feeling at home somewhere, Anne finds in property hunting what she once loved in music: the search for the perfect harmony.
Understanding a person, capturing their desires, finding the right note between dream and reality — that’s what drives her.

At Perle Rare, Anne brings her sensitivity, energy, and unique perspective to those seeking a place that truly reflects who they are.
Because ultimately, finding a home is a bit like composing a new life’s melody.'
 WHERE CP_RefConseiller = 278 AND CP_Actif = 1;

-- 280 : Clémence Kamienny
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/clemence-kamienny-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/clemence-kamienny-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Diplômée de l’emlyon business school, Clémence Kamienny a débuté sa carrière comme consultante en digital customer. Elle y a développé une solide expertise dans l’analyse des besoins, l’accompagnement sur mesure et la conduite de projets complexes avec exigence et méthode.

Parallèlement, ses propres expériences d’acquisition immobilière lui ont permis de se confronter très tôt aux réalités du marché, aux enjeux patrimoniaux et aux défis que rencontrent les acheteurs. Cette double perspective — professionnelle et personnelle — est aujourd’hui au cœur de son approche de Conseiller Personnel chez Perle Rare.

Elle met au service de ses clients son approche orientée expérience client, héritée de son parcours en conseil, qui garantit fluidité, transparence et sérénité tout au long du processus d’acquisition.

Clémence vous permettra de transformer un projet d’achat en une expérience positive, fluide et marquante, où chaque étape est anticipée et chaque détail compte pour trouver votre Perle Rare !',
       CP_parcours_en = 'A graduate of emlyon business school, Clémence Kamienny began her career as a digital customer consultant. In this role, she developed strong expertise in needs analysis, bespoke client support, and the management of complex projects with rigor and precision.

At the same time, her own real estate acquisition experiences allowed her to gain early, hands-on exposure to market realities, wealth management considerations, and the challenges faced by buyers. This dual perspective—both professional and personal—now lies at the heart of her approach as a Personal Advisor at Perle Rare.

She brings to her clients a customer-experience-driven approach, inherited from her consulting background, ensuring clarity, transparency, and peace of mind throughout the entire acquisition process.

Clémence helps turn a property purchase into a positive, seamless, and memorable experience, where every step is anticipated and every detail matters in finding your Perle Rare!'
 WHERE CP_RefConseiller = 280 AND CP_Actif = 1;

-- 282 : Dessislava Pavlova
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/dessislava-pavlova-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/dessislava-pavlova-property-hunter-personal-consultant-perle-rare/',
       CP_parcours_fr = 'D’origine bulgare, Dessislava a construit un parcours riche et international, nourri par la mode, l’entrepreneuriat et une passion affirmée pour l’immobilier. Son expérience dans l’univers exigeant de la mode lui a apporté un sens aigu du détail, de l’esthétique et des attentes clients — des qualités précieuses qu’elle met aujourd’hui au service de la chasse immobilière.

Attentive aux lignes, aux volumes et à l’harmonie d’un lieu, Dessislava perçoit un bien immobilier comme une pièce unique, à révéler et à faire correspondre à une personnalité, un style de vie, une ambition. Sa sensibilité, alliée à une grande rigueur et à un excellent sens de la relation, lui permet d’accompagner chaque client avec justesse et engagement.

En rejoignant Perle Rare, Dessislava trouve un cadre en parfaite adéquation avec ses valeurs : l’exigence, l’élégance, l’écoute et la recherche du détail qui fait toute la différence. Elle s’investit pleinement pour guider ses clients vers des biens qui leur ressemblent vraiment — leur perle rare.',
       CP_parcours_en = 'Originally from Bulgaria, Dessislava has built a rich and international career shaped by fashion, entrepreneurship, and a strong passion for real estate. Her experience in the demanding world of fashion has given her a keen eye for detail, aesthetics, and client expectations—valuable qualities she now brings to her work as a property hunter.

Attentive to lines, volumes, and the harmony of a space, Dessislava views each property as a unique piece to be revealed and matched with a personality, a lifestyle, and an ambition. Her sensitivity, combined with great rigor and an excellent sense of client relationships, allows her to support each client with precision and genuine commitment.

By joining Perle Rare, Dessislava found an environment perfectly aligned with her values: high standards, elegance, attentive listening, and the pursuit of the detail that makes all the difference. She is deeply committed to guiding her clients toward properties that truly reflect who they are—their own perle rare.'
 WHERE CP_RefConseiller = 282 AND CP_Actif = 1;

-- 290 : Elsa Pons
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/elsa-pons/',
       CP_pageWebEn = 'https://perle-rare.com/en/personal-consultant-perle-rare-elsa-pons/',
       CP_parcours_fr = 'Elsa, l’œil et la rigueur au service de votre recherche

Après une formation en droit des affaires et un Master à l’Université Paris Dauphine, Elsa a construit l’essentiel de son parcours professionnel dans l’univers des médias et de l’audiovisuel.

Pendant plus de quinze ans, elle a développé une véritable expertise dans la recherche, l’analyse et la sélection de programmes, en France comme à l’international. Un métier qui lui a appris à explorer un marché en profondeur, à détecter les opportunités, à faire des choix exigeants mais aussi à négocier pour mener à bien ses projets.

Des réflexes qu’elle transpose aujourd’hui naturellement à la recherche immobilière.

Curieuse, organisée et persévérante, Elsa aime chercher, comparer et aller au-delà des premières impressions. Elle prend le temps de comprendre précisément les attentes de ses clients afin de cibler les biens qui leur correspondent réellement et de les accompagner avec attention tout au long de leur projet.

Son expérience de la négociation et sa formation juridique lui apportent également la rigueur et le sens de l’analyse indispensables pour aborder sereinement les différentes étapes d’une acquisition.

Sa force : son œil exercé et sa capacité à chercher là où les autres ne regardent pas forcément, pour identifier parmi de nombreuses possibilités celle qui pourra devenir la perle rare de ses clients.',
       CP_parcours_en = 'Elsa, a discerning eye and a rigorous approach dedicated to your property search

After studying business law and completing a Master’s degree at Paris Dauphine University, Elsa built most of her professional career in the media and broadcasting industry.

For over fifteen years, she developed extensive expertise in researching, analysing and selecting programmes, both in France and internationally. This experience taught her how to explore a market in depth, identify opportunities, make discerning choices and negotiate effectively to bring her projects to fruition.

Skills and instincts that she now naturally applies to property searches.

Curious, organised and persistent, Elsa enjoys searching, comparing and looking beyond first impressions. She takes the time to fully understand her clients’ expectations in order to identify properties that genuinely suit them and guide them with care throughout every stage of their project.

Her experience in negotiation and her legal background also provide her with the rigour and analytical skills required to navigate the different stages of a property acquisition with confidence.

Her strength: her discerning eye and her ability to look where others may not, enabling her to identify, among numerous possibilities, the property that could become her clients’ very own perle rare.'
 WHERE CP_RefConseiller = 290 AND CP_Actif = 1;

-- 305 : Chloé Poissonnet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/chloe-poissonnet-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/chloe-poissonnet-property-hunter-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Passionnée par l’immobilier et animée par l’envie d’accompagner chacun dans la concrétisation de son projet de vie, Chloé rejoint Perle Rare en tant que chasseur immobilier. Forte d’une expérience solide dans la gestion locative et la relation client, elle a développé une connaissance fine des marchés, une grande rigueur professionnelle et une vraie capacité à comprendre les besoins spécifiques de chaque client.

Chloé a enrichi son parcours par une formation spécialisée en management immobilier, ce qui lui permet d’aborder chaque recherche de bien avec méthode, écoute et sens de l’organisation — des atouts essentiels pour trouver des lieux qui allient potentiel, charme et adéquation avec les attentes de vie de ses clients.

Chez Perle Rare, Chloé met toute son énergie et sa passion au service de ceux qui souhaitent aller au-delà d’une simple transaction : trouver un lieu unique, pensé pour être habité et aimé.',
       CP_parcours_en = 'Passionate about real estate and driven by a desire to support each individual in bringing their life project to fruition, Chloé joined Perle Rare as a property hunter. With solid experience in rental management and client relations, she has developed a strong understanding of the market, great professional rigor, and a genuine ability to grasp the specific needs of each client.

Chloé has further strengthened her background with specialized training in real estate management, enabling her to approach every property search with method, attentive listening, and a strong sense of organization—essential assets for identifying homes that combine potential, charm, and true alignment with her clients’ lifestyle expectations.

At Perle Rare, Chloé dedicates all her energy and passion to those seeking more than a simple transaction: finding a unique place, designed to be lived in and truly loved.'
 WHERE CP_RefConseiller = 305 AND CP_Actif = 1;

-- 322 : Laurence Pinaire
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/laurence-pinaire/',
       CP_pageWebEn = 'https://perle-rare.com/en/laurence-pinaire-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Laurence, une expertise éditoriale au service de la valorisation des projets immobiliers.

Forte de plus de 15 ans d’expérience dans la communication, Laurence apporte une dimension essentielle au métier de chasseur immobilier : celle du regard, du récit et de la mise en valeur.

Dans un marché où l’émotion et la perception jouent un rôle clé, elle sait capter l’essence d’un lieu, en comprendre les usages et en exprimer le potentiel. Son expertise contribue à affiner la lecture des biens, à mieux projeter les acquéreurs et à donner du sens à chaque projet immobilier.

Habituée à travailler sur des enjeux de positionnement et d’image, Laurence accompagne les recherches avec une attention particulière portée à la cohérence entre le bien, son environnement et les attentes de ses futurs occupants. Elle participe ainsi à créer une expérience plus fluide, plus lisible et plus alignée avec les aspirations de chacun.

À la croisée de la communication et de l’immobilier, elle enrichit l’approche du métier de chasseur en y intégrant une sensibilité aux détails qui font toute la différence pour trouver votre perle rare !',
       CP_parcours_en = 'Laurence, editorial expertise serving the enhancement of real estate projects.

With over 15 years of experience in communication, Laurence brings an essential dimension to the role of a property finder: perspective, storytelling, and presentation.

In a market where emotion and perception play a key role, she knows how to capture the essence of a place, understand how it is used, and express its full potential. Her expertise helps refine the reading of properties, enables buyers to better envision themselves, and brings meaning to each real estate project.

Accustomed to working on positioning and brand image challenges, Laurence supports each search with particular attention to the alignment between the property, its environment, and the expectations of its future occupants. In doing so, she helps create a smoother, clearer, and more coherent experience, aligned with each client’s aspirations.

At the crossroads of communication and real estate, she enriches the property search approach by integrating a sensitivity to the details that make all the difference in finding your perle rare !'
 WHERE CP_RefConseiller = 322 AND CP_Actif = 1;

-- 339 : Lionel Jamet
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/lionel-jamet/',
       CP_pageWebEn = 'https://perle-rare.com/en/lionel-jamet-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Lionel, une expertise financière au service de l’immobilier.

Fort d’un parcours construit au sein de la banque et de la gestion de patrimoine, Lionel apporte une lecture fine et structurée des enjeux financiers liés à l’immobilier.

Habitué à accompagner des clients exigeants dans la gestion et l’optimisation de leur patrimoine, Lionel développe une approche sur-mesure, fondée sur l’écoute et la compréhension des objectifs. Il sait traduire des problématiques complexes en solutions claires, pertinentes et durables.

À la croisée des mondes bancaire, patrimonial et immobilier, il incarne une vision globale où chaque décision s’inscrit dans une stratégie cohérente. Son regard, à la fois analytique et pragmatique, permet d’éclairer les choix et de sécuriser les parcours.

Engagé et attentif à la qualité de la relation, Lionel accompagne chaque projet avec exigence et discrétion, dans une logique de confiance pour trouver votre perle rare !',
       CP_parcours_en = 'Lionel, financial expertise serving real estate.

With a career built in banking and wealth management, Lionel brings a precise and structured understanding of the financial challenges related to real estate.

Accustomed to supporting demanding clients in managing and optimizing their assets, Lionel has developed a tailored approach grounded in listening and a deep understanding of each client’s objectives. He is skilled at translating complex issues into clear, relevant, and sustainable solutions.

At the crossroads of banking, wealth management, and real estate, he embodies a holistic vision in which every decision is part of a coherent strategy. His perspective, both analytical and pragmatic, helps clarify choices and secure each step of the journey.

Committed and attentive to the quality of relationships, Lionel supports every project with high standards and discretion, building trust to help you find your perle rare !'
 WHERE CP_RefConseiller = 339 AND CP_Actif = 1;

-- 343 : Marta Cléry
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/marta-clery-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/personal-consultant-perle-rare-marta-clery/',
       CP_parcours_fr = 'Marta, un regard international au service de votre recherche immobilière.

Forte d’un parcours riche entre marketing, entrepreneuriat et immobilier, Marta a toujours placé l’écoute, le relationnel et l’accompagnement au cœur de son activité professionnelle.

Espagnole installée à Paris depuis de nombreuses années, elle a notamment accompagné pendant près de dix ans des cadres internationaux et leurs familles dans leur installation à Paris. Une expérience qui lui a permis de développer une excellente compréhension des attentes d’une clientèle française comme internationale, mais aussi une vraie capacité à appréhender un projet immobilier dans sa globalité.

Aujourd’hui, elle met cette expérience, sa connaissance de Paris et son goût du contact humain au service de ses clients et de leur recherche immobilière.

À l’écoute, dynamique et engagée, Marta aime comprendre ce qui se cache derrière chaque recherche : un mode de vie, des habitudes, des envies et parfois un nouveau départ. Elle s’attache à cerner précisément les attentes de ses clients pour leur proposer des biens qui leur correspondent réellement et les accompagner avec attention à chaque étape de leur projet.

Sa force : son regard international et sa capacité à créer rapidement une relation de confiance. Marta accompagne chaque recherche avec énergie et implication, pour permettre à ses clients de trouver bien plus qu’un appartement : un lieu dans lequel ils se sentiront chez eux.',
       CP_parcours_en = 'Marta, an international perspective dedicated to your property search

With a diverse background spanning marketing, entrepreneurship and real estate, Marta has always placed listening, personal relationships and client support at the heart of her professional approach.

Originally from Spain and based in Paris for many years, she spent nearly a decade assisting international executives and their families with their relocation to Paris. This experience gave her an excellent understanding of the expectations of both French and international clients, as well as the ability to approach each property project from a broader perspective.

Today, she brings this experience, her knowledge of Paris and her natural affinity for connecting with people to each of her clients and their property search.

Attentive, dynamic and committed, Marta likes to understand what lies behind every search: a lifestyle, daily habits, personal aspirations and, sometimes, an entirely new chapter in life. She takes the time to understand her clients’ expectations in detail, enabling her to identify properties that genuinely suit them and guide them with care throughout every stage of their project.

Her strength: her international perspective and her ability to quickly build a relationship based on trust. Marta approaches every search with energy and dedication, helping her clients find much more than an apartment: a place they can truly call home.'
 WHERE CP_RefConseiller = 343 AND CP_Actif = 1;

-- 347 : Roxane Baché
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/roxane-bache-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/roxane-bache-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Roxane, une expertise à la croisée de l’immobilier et des belles marques.

Avec une solide culture des nouveaux usages et tendances émergentes, Roxane apporte un regard singulier sur l’immobilier. Son parcours, nourri par le conseil en stratégie et l’analyse des comportements consommateurs, lui permet de comprendre en profondeur les attentes d’une clientèle exigeante et en constante évolution.

À la croisée de l’immobilier et de l’univers des belles marques, elle développe une approche où le sens du détail, l’expérience client et la valorisation des lieux occupent une place centrale. Habituée à décrypter les codes du retail, du luxe et du lifestyle, Roxane sait identifier ce qui fait la singularité d’un bien.

Son expertise ne se limite pas à la connaissance du marché : elle s’appuie sur une vision stratégique, inspirée des meilleures pratiques internationales, pour accompagner chaque projet avec justesse et pertinence.

Curieuse, engagée et résolument tournée vers l’avenir, Roxane incarne une nouvelle génération de professionnels capables de faire dialoguer immobilier, marque et expérience pour trouver votre perle rare !',
       CP_parcours_en = 'Roxane, an expertise at the crossroads of real estate and premium brands.

With a strong understanding of emerging trends and evolving lifestyles, Roxane brings a distinctive perspective to real estate. Her background in strategy consulting and consumer behavior analysis enables her to deeply understand the expectations of a demanding and constantly changing clientele.

At the intersection of real estate and the world of premium brands, she has developed an approach where attention to detail, client experience, and the enhancement of spaces are central. Well-versed in decoding the codes of retail, luxury, and lifestyle, Roxane has a keen ability to identify what makes a property truly unique.

Her expertise goes beyond market knowledge: it is grounded in a strategic vision, inspired by international best practices, allowing her to support each project with accuracy and relevance.

Curious, committed, and forward-thinking, Roxane embodies a new generation of professionals capable of bridging real estate, brand, and experience to help you find your perle rare !'
 WHERE CP_RefConseiller = 347 AND CP_Actif = 1;

-- 356 : Florence Roux de Bézieux
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/conseiller-personnel-florence-roux-de-bezieux-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/personal-consultant-florence-roux-de-bezieux-perle-rare/',
       CP_parcours_fr = 'Florence, le sens du contact au service de votre recherche immobilière

Forte d’une solide expérience dans le conseil en communication, Florence a participé au développement d’agences et de médias ainsi qu’à la création de services destinés aux professionnels de l’immobilier. Un parcours qui lui a permis de développer un véritable talent pour créer des connexions, identifier les opportunités et comprendre rapidement les enjeux de chaque projet.

Aujourd’hui, elle met cette expérience au service de ses clients et de leur recherche immobilière. Sa connaissance des réseaux, son goût du contact humain et sa capacité à ouvrir les bonnes portes lui permettent d’explorer chaque piste avec curiosité et efficacité.

À l’écoute, persévérante et réactive, Florence s’attache à comprendre précisément les attentes de chacun pour mener une recherche ciblée et proposer des biens réellement en adéquation avec le projet de ses clients.

Sa force : son énergie positive et son sens du relationnel. Florence accompagne chaque recherche avec engagement et bienveillance, pour faire gagner du temps à ses clients et les guider, étape après étape, jusqu’à leur perle rare !',
       CP_parcours_en = 'Florence, a natural connector dedicated to your property search

With a strong background in communications consulting, Florence has contributed to the development of agencies and media companies, as well as to the creation of services for real estate professionals. This experience has given her a genuine talent for building connections, identifying opportunities and quickly understanding the specific challenges of each project.

Today, she puts this expertise at the service of her clients and their property search. Her extensive network, natural people skills and ability to open the right doors allow her to explore every opportunity with curiosity and efficiency.

Attentive, persistent and responsive, Florence takes the time to fully understand each client’s expectations in order to conduct a focused search and identify properties that truly match their needs and aspirations.

Her strength: her positive energy and natural ability to connect with people. Florence approaches every search with commitment and care, helping her clients save valuable time and guiding them every step of the way until they find their perle rare !'
 WHERE CP_RefConseiller = 356 AND CP_Actif = 1;

-- 357 : Isabelle Chetochine
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/isabelle-chetochine/',
       CP_pageWebEn = 'https://perle-rare.com/en/isabelle-chetochine-personal-consultant/',
       CP_parcours_fr = 'Depuis plusieurs années, Isabelle accompagne ses clients dans leurs projets immobiliers haut de gamme. Grâce à sa parfaite connaissance du marché et de ses acteurs, elle sait identifier les biens qui se distinguent par leur emplacement, leur qualité ou leur potentiel.

Au-delà des critères classiques, elle s’attache à trouver des lieux de vie en accord avec les attentes et le mode de vie de ses clients.

Habituée aux recherches exigeantes et souvent confidentielles, Isabelle met à chaque étape son écoute, sa discrétion et sa réactivité au service de ses clients. Son expertise lui permet d’anticiper les opportunités et de proposer un accompagnement personnalisé, rigoureux et efficace.

En tant que chasseuse immobilière, elle a à cœur de créer la rencontre idéale entre un bien et un projet de vie, afin d’aider chacun trouver sa perle rare.',
       CP_parcours_en = 'For several years, Isabelle has been guiding her clients through their high-end real estate projects. Thanks to her thorough knowledge of the market and its players, she knows how to identify properties that stand out for their location, quality or potential.

Beyond the classic criteria, she is committed to finding living spaces that align with her clients’ expectations and lifestyle.

Accustomed to demanding and often confidential searches, Isabelle brings her attentiveness, discretion and responsiveness to every step of the process. Her expertise allows her to anticipate opportunities and offer a personalized, rigorous and efficient service.

As a property hunter, she is dedicated to creating the perfect match between a property and a life project, helping each client find their « perle rare ».'
 WHERE CP_RefConseiller = 357 AND CP_Actif = 1;

-- 367 : Diane de Monteynard
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/diane-de-monteynard-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/diane-de-monteynard-personal-consultant-perle-rare-en/',
       CP_parcours_fr = 'C’est aux États-Unis que Diane découvre l’immobilier et se forme à ce métier avec une approche résolument tournée vers le service et l’accompagnement personnalisé des acquéreurs.

De retour à Paris, elle fonde sa structure de chasse immobilière qu’elle dirige durant dix ans, avant de repartir pour Singapour et suivre une formation d’architecte d’intérieur.

Son parcours se poursuit pendant sept années au sein de la Maison Junot, référence de l’immobilier haut de gamme parisien, où elle enrichit son expertise de la transaction et approfondit sa connaissance du marché parisien.

En rejoignant Perle Rare, Diane revient à sa vocation première : représenter exclusivement les intérêts des acquéreurs et les accompagner dans la recherche du bien qui leur correspond véritablement.

Forte de cette double expérience en chasse immobilière et en transaction, elle porte un regard averti sur les biens, leur potentiel et leur juste valeur. Convaincue qu’un projet immobilier va bien au-delà d’une simple acquisition, elle privilégie une relation fondée sur l’écoute, la confiance et le conseil.

Ultra-réactive, engagée et attentive à la dimension humaine de chaque mission, Diane met son expérience et sa connaissance du marché au service de ses clients, avec une ambition constante : leur permettre de concrétiser leur projet immobilier en toute sérénité.

Sa force : une expertise de 20 ans dans l’immobilier, enrichie par plusieurs expatriations et un engagement sans réserve dans la défense des intérêts de ses clients.',
       CP_parcours_en = 'It was in the United States that Diane discovered the world of real estate and trained in the profession, developing an approach firmly rooted in service and personalised support for buyers.

Upon returning to Paris, she founded her own property search company, which she successfully ran for ten years, before moving to Singapore, where she pursued training in interior design.

Her career then continued with seven years at Maison Junot, a leading name in Parisian luxury real estate, where she further developed her expertise in property transactions and deepened her knowledge of the Paris property market.

By joining Perle Rare, Diane returns to her original calling: representing the exclusive interests of buyers and guiding them in finding a property that truly meets their needs and aspirations.

Drawing on her extensive experience in both property search and real estate transactions, she brings a discerning eye to every property, assessing its potential and true market value. Convinced that a property purchase is about far more than simply acquiring a home, she places listening, trust and expert advice at the heart of every client relationship.

Highly responsive, dedicated and deeply attentive to the human dimension of every project, Diane puts her experience and market knowledge at the service of her clients, with one constant ambition: to help them achieve their property goals with complete peace of mind.

Her strength: 20 years of real estate expertise, enriched by several international experiences and an unwavering commitment to protecting her clients’ best interests.'
 WHERE CP_RefConseiller = 367 AND CP_Actif = 1;

-- 374 : Solveig de Fontgalland
UPDATE conseillers_personnels
   SET CP_pageWebFr = 'https://perle-rare.com/fr/solveig-de-fontgalland-conseiller-personnel-perle-rare/',
       CP_pageWebEn = 'https://perle-rare.com/en/solveig-de-fontgalland-personal-consultant-perle-rare/',
       CP_parcours_fr = 'Parisienne de naissance, Solveig a passé près de vingt ans en Asie, où elle a évolué dans les univers du luxe, de la création et de l’art de vivre.

Son parcours dans la création d’univers parfumés et d’objets lifestyle a affiné sa sensibilité esthétique et son sens du détail. Familière de l’architecture, de la décoration et des chantiers de rénovation depuis l’enfance, elle porte sur les lieux un regard à la fois sensible et pragmatique. Attentive aux volumes, aux matières et à l’atmosphère d’un appartement, elle en apprécie les qualités tout en tenant compte de ses contraintes et de ses possibilités d’aménagement.

Pour elle, un bien immobilier ne se résume pas à ses caractéristiques : il doit avant tout correspondre à une personnalité, à un mode de vie et à des aspirations.

Son expérience internationale et sa maîtrise du français, de l’anglais et du chinois lui permettent d’accompagner une clientèle française et étrangère en tenant compte de ses repères culturels et de ses modes de vie.

Chez Perle Rare, Solveig privilégie l’écoute, la confiance et un accompagnement personnalisé pour trouver le bien qui répond véritablement aux attentes de chacun.

Sa force : un regard exercé sur les lieux et leur potentiel, une ouverture internationale et une attention particulière à la dimension humaine de chaque projet.',
       CP_parcours_en = 'Born and raised in Paris, Solveig spent nearly twenty years in Asia, building her career in the worlds of luxury, design and lifestyle.

Her experience creating fragrances and lifestyle products has refined her aesthetic sensibility and keen eye for detail. Surrounded by architecture, interior design and renovation projects from an early age, she has developed an approach to properties that is both intuitive and pragmatic. Attentive to proportions, materials and the atmosphere of a home, she appreciates its unique qualities while carefully considering its constraints and potential for transformation.

For Solveig, a property is far more than a collection of features: above all, it should reflect a personality, a lifestyle and individual aspirations.

Her international experience and fluency in French, English and Chinese enable her to guide both French and international clients with a deep understanding of their cultural backgrounds and lifestyles.

At Perle Rare, Solveig places listening, trust and personalised service at the heart of her approach, helping each client find a property that truly meets their expectations.

Her strength: a discerning eye for properties and their potential, an international outlook and a genuine commitment to the human dimension of every real estate project.'
 WHERE CP_RefConseiller = 374 AND CP_Actif = 1;

COMMIT;

-- Contrôle après :
--   SELECT COUNT(*) AS total_rows,
--          SUM(CP_pageWebFr IS NOT NULL) AS filled_fr_url,
--          SUM(CP_pageWebEn IS NOT NULL) AS filled_en_url,
--          SUM(CP_parcours_fr IS NOT NULL) AS filled_fr_bio,
--          SUM(CP_parcours_en IS NOT NULL) AS filled_en_bio
--     FROM conseillers_personnels;
--   -- attendu : 379 total, 40/40/40/40 renseignés
--   SELECT COUNT(*) AS backup_rows FROM conseillers_personnels_bak_20261120_pageweb_parcours;
--   -- attendu : 379
