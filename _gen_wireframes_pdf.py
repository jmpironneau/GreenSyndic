"""
GreenSyndic — Wireframes des 4 applications
Generateur PDF avec wireframes filaires ASCII
"""
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.colors import HexColor, black, white, grey
from reportlab.lib.units import mm, cm
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak,
    Table, TableStyle, Preformatted, KeepTogether
)
from reportlab.pdfgen import canvas
import os

OUTPUT = os.path.join(os.path.dirname(__file__),
    "Additional Data", "Docs & Marketing", "GreenSyndic_Wireframes_4Apps.pdf")

GREEN = HexColor("#2e7d32")
DARK = HexColor("#1b5e20")
LIGHT_GREEN = HexColor("#e8f5e9")
LIGHT_GREY = HexColor("#f5f5f5")
BORDER = HexColor("#cccccc")

styles = getSampleStyleSheet()

# Custom styles
styles.add(ParagraphStyle("CoverTitle", parent=styles["Title"],
    fontSize=28, textColor=GREEN, spaceAfter=10, alignment=TA_CENTER))
styles.add(ParagraphStyle("CoverSubtitle", parent=styles["Normal"],
    fontSize=14, textColor=grey, alignment=TA_CENTER, spaceAfter=6))
styles.add(ParagraphStyle("AppTitle", parent=styles["Heading1"],
    fontSize=22, textColor=GREEN, spaceBefore=20, spaceAfter=10))
styles.add(ParagraphStyle("ScreenTitle", parent=styles["Heading2"],
    fontSize=14, textColor=DARK, spaceBefore=14, spaceAfter=6))
styles.add(ParagraphStyle("Desc", parent=styles["Normal"],
    fontSize=9, leading=12, spaceAfter=4, textColor=HexColor("#333333")))
styles.add(ParagraphStyle("Wire", parent=styles["Code"],
    fontSize=7.5, leading=9, fontName="Courier", backColor=LIGHT_GREY,
    borderColor=BORDER, borderWidth=1, borderPadding=6,
    spaceBefore=4, spaceAfter=8))
styles.add(ParagraphStyle("EndpointStyle", parent=styles["Normal"],
    fontSize=8, leading=10, textColor=HexColor("#666666"),
    leftIndent=12, spaceAfter=2))
styles.add(ParagraphStyle("TOCItem", parent=styles["Normal"],
    fontSize=11, leading=16, leftIndent=20))
styles.add(ParagraphStyle("TOCApp", parent=styles["Normal"],
    fontSize=13, leading=20, textColor=GREEN, fontName="Helvetica-Bold"))
styles.add(ParagraphStyle("SectionIntro", parent=styles["Normal"],
    fontSize=10, leading=13, spaceAfter=10, textColor=HexColor("#444444")))
styles.add(ParagraphStyle("Legend", parent=styles["Normal"],
    fontSize=8, leading=10, textColor=HexColor("#888888"), spaceAfter=2))


def screen(title, description, wireframe, endpoints=None):
    """Build a screen section."""
    elems = []
    elems.append(Paragraph(title, styles["ScreenTitle"]))
    elems.append(Paragraph(description, styles["Desc"]))
    elems.append(Preformatted(wireframe, styles["Wire"]))
    if endpoints:
        elems.append(Paragraph("<b>Endpoints API :</b>", styles["Legend"]))
        for ep in endpoints:
            elems.append(Paragraph(ep, styles["EndpointStyle"]))
    elems.append(Spacer(1, 8))
    return KeepTogether(elems)


def build_pdf():
    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    doc = SimpleDocTemplate(OUTPUT, pagesize=A4,
        topMargin=2*cm, bottomMargin=2*cm,
        leftMargin=2*cm, rightMargin=2*cm)
    story = []

    # =========================================================
    # COVER PAGE
    # =========================================================
    story.append(Spacer(1, 80))
    story.append(Paragraph("GreenSyndic", styles["CoverTitle"]))
    story.append(Paragraph("Wireframes des 4 Applications", styles["CoverSubtitle"]))
    story.append(Spacer(1, 20))
    story.append(Paragraph("DeskSyndic | MobSyndic | MobProprio | MobLoc", styles["CoverSubtitle"]))
    story.append(Spacer(1, 30))

    cover_data = [
        ["Application", "Type", "Port", "Utilisateurs"],
        ["DeskSyndic", "Desktop MVC", ":5051", "Syndic, Comptable, DAF"],
        ["MobSyndic", "PWA Mobile", ":5052", "Gestionnaire terrain"],
        ["MobProprio", "PWA Mobile", ":5053", "Proprietaires (~400)"],
        ["MobLoc", "PWA Mobile", ":5054", "Locataires (~300)"],
    ]
    t = Table(cover_data, colWidths=[100, 90, 60, 200])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), GREEN),
        ("TEXTCOLOR", (0, 0), (-1, 0), white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("GRID", (0, 0), (-1, -1), 0.5, BORDER),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [white, LIGHT_GREEN]),
        ("ALIGN", (2, 0), (2, -1), "CENTER"),
    ]))
    story.append(t)
    story.append(Spacer(1, 30))
    story.append(Paragraph("Backend API : GreenSyndic.Api sur le port :5050", styles["CoverSubtitle"]))
    story.append(Paragraph("Mars 2026 — v1.0", styles["CoverSubtitle"]))
    story.append(PageBreak())

    # =========================================================
    # TABLE OF CONTENTS
    # =========================================================
    story.append(Paragraph("Table des matieres", styles["Title"]))
    story.append(Spacer(1, 10))

    toc = [
        ("1. DeskSyndic — Desktop Syndic", [
            "1.1 Dashboard", "1.2 Gestion des lots", "1.3 Fiche lot",
            "1.4 Comptabilite — Ecritures", "1.5 Comptabilite — Grand livre",
            "1.6 Appels de fonds", "1.7 Impayes et relances",
            "1.8 Assemblees Generales — Liste", "1.9 AG — Animation / Votes",
            "1.10 Gestion locative — Baux", "1.11 Baux commerciaux OHADA",
            "1.12 Candidatures locataires", "1.13 Travaux et incidents",
            "1.14 Ordres de service", "1.15 Communication multicanal",
            "1.16 GED — Documents", "1.17 Reporting / KPI",
            "1.18 Parametrage general", "1.19 Parametrage structure",
            "1.20 Gestion utilisateurs / Roles",
            "1.21 Veille juridique", "1.22 Export / Import Excel",
            "1.23 Annuaire"
        ]),
        ("2. MobSyndic — PWA Syndic Terrain", [
            "2.1 Dashboard KPI", "2.2 Creer incident (photo+GPS)",
            "2.3 VTI — Visite Technique", "2.4 Liste impayes",
            "2.5 Validation factures", "2.6 Fiche lot resumee",
            "2.7 Etat des lieux mobile", "2.8 Scan facture OCR",
            "2.9 Notifications"
        ]),
        ("3. MobProprio — PWA Proprietaire", [
            "3.1 Accueil", "3.2 Mon compte",
            "3.3 Payer charges", "3.4 Signaler incident",
            "3.5 Documents", "3.6 Messages",
            "3.7 Voter AG", "3.8 Mon immeuble",
            "3.9 CRG Bailleur", "3.10 Profil"
        ]),
        ("4. MobLoc — PWA Locataire", [
            "4.1 Accueil", "4.2 Mon loyer",
            "4.3 Quittances", "4.4 Signaler incident",
            "4.5 Documents", "4.6 Contact gestionnaire",
            "4.7 Infos pratiques", "4.8 Regularisation charges",
            "4.9 Profil"
        ]),
    ]
    for app_title, items in toc:
        story.append(Paragraph(app_title, styles["TOCApp"]))
        for item in items:
            story.append(Paragraph(item, styles["TOCItem"]))
    story.append(PageBreak())

    # =========================================================
    # 1. DESKSYNDIC
    # =========================================================
    story.append(Paragraph("1. DeskSyndic — Desktop Syndic (:5051)", styles["AppTitle"]))
    story.append(Paragraph(
        "Application desktop complete pour le syndic professionnel. "
        "Comptabilite SYSCOHADA, AG, gestion locative residentielle et commerciale, "
        "travaux, communication multicanal, reporting 50+ KPI, parametrage integral. "
        "Ecrans larges >= 1024px. Razor MVC + JavaScript.",
        styles["SectionIntro"]))

    # 1.1 Dashboard
    story.append(screen(
        "1.1 Dashboard Syndic",
        "Tableau de bord principal avec KPI, graphiques, agenda et alertes. "
        "Vue consolidee multi-coproprietes.",
        """\
+============================================================================+
| [Logo GreenSyndic]  Accueil | Lots | Compta | AG | Travaux | Comm | [User] |
+============================================================================+
|                                                                            |
|  +--- KPI CARDS (4 colonnes) ----------------------------------------+    |
|  | IMPAYES         | LOTS VACANTS    | INCIDENTS      | PROCHAINE AG  |    |
|  | 4 250 000 FCFA  | 12 (taux 95%)   | 7 dont 2 urg.  | 15/04 Horiz. |    |
|  | [^] +3%         | [v] -1%         | [^] +2          | dans 25j     |    |
|  +------------------------------------------------------------------+     |
|                                                                            |
|  +--- COL GAUCHE (30%) ---+  +--- COL DROITE (70%) ------------------+    |
|  | TRESORERIE              |  | AGENDA DU JOUR                        |    |
|  | 12 500 000 FCFA         |  | 09h00 — RDV Fournisseur Acajou       |    |
|  | [sparkline 12 mois]     |  | 11h00 — Visite lot V-023             |    |
|  |                         |  | 14h00 — Reunion conseil syndical     |    |
|  | TAUX OCCUPATION         |  +---------------------------------------+    |
|  | 94% [barre progress.]   |  | DERNIERES DEMANDES                    |    |
|  |                         |  | M. Kouame — Fuite eau V-012 — 2h     |    |
|  | RECETTES MOIS           |  | Mme Traore — Bruit voisin A-AC-302   |    |
|  | 8 700 000 / 9 200 000   |  | COSMOS — Facture GE en attente       |    |
|  |                         |  +---------------------------------------+    |
|  | BAUX A ECHEANCE         |  | GRAPHIQUE TRESORERIE (12 mois)        |    |
|  | 3 baux < 90j            |  | [===barres Chart.js================]  |    |
|  +-------------------------+  +---------------------------------------+    |
+============================================================================+""",
        ["GET /api/dashboard/kpis", "GET /api/incidents?status=open",
         "GET /api/leases?expiringDays=90", "GET /api/payments/summary"]))

    # 1.2 Gestion des lots
    story.append(screen(
        "1.2 Gestion des lots — Liste",
        "Referentiel unique : immeubles, batiments et lots. Filtres multi-criteres, "
        "recherche, export Excel. Bouton [+ Ajouter un lot].",
        """\
+============================================================================+
| Lots > Liste des lots                            [+ Ajouter]  [Export XLS] |
+----------------------------------------------------------------------------+
| Filtre: [Copropriete v] [Batiment v] [Type v] [Statut v]   [Rechercher___] |
+----------------------------------------------------------------------------+
| #     | Batiment       | Type    | Surface | Tantiemes | Proprio.   | Stat.|
|-------|----------------|---------|---------|-----------|------------|------|
| V-001 | Zone Villas    | F4 Minan| 185 m2  | 450       | M. Koffi   | Occ. |
| V-002 | Zone Villas    | F5 Asue | 223 m2  | 520       | M. Coulibaly| Occ.|
| A-AC-101| Imm. Acajou  | F3      | 97 m2   | 180       | Mme Bamba  | Loue |
| A-AC-102| Imm. Acajou  | F4      | 131 m2  | 240       | SCI Ivoire | Vac. |
| C-01  | COSMOS         | Commerce| 2370 m2 | 1200      | Green Shops| Occ. |
|-------|----------------|---------|---------|-----------|------------|------|
|                          Page 1/14   [<] [1] [2] [3] ... [14] [>]          |
+============================================================================+""",
        ["GET /api/units?page=1&size=20", "GET /api/buildings", "GET /api/co-ownerships"]))

    # 1.3 Fiche lot
    story.append(screen(
        "1.3 Fiche lot — Detail",
        "Fiche complete d'un lot : infos, proprietaire, locataire, compteurs, "
        "historique interventions, mutations. Carnet d'entretien numerique.",
        """\
+============================================================================+
| Lot V-012 — Villa F5 Asue                            [Modifier] [Hist.]   |
+----------------------------------------------------------------------------+
| INFORMATIONS            | PROPRIETAIRE           | OCCUPATION              |
| Type: Villa F5 Asue     | M. Jean-Marc Kouame    | Statut: Occupe proprio  |
| Surface: 223 m2         | Tel: +225 07 XX XX XX  | Depuis: 15/03/2024      |
| Tantiemes horiz: 520    | Email: jm@mail.ci      | Locataire: —            |
| Batiment: Zone Villas   | Membre CS: Oui         |                         |
| Copro H: Green City     |                        |                         |
+----------------------------------------------------------------------------+
| SOLDE CHARGES           | COMPTEURS              | EQUIPEMENTS             |
| Charges H: 0 FCFA       | Eau: 12345 (01/03)     | Piscine: Oui            |
| Charges V: N/A           | Elec: 67890 (01/03)    | Jardin: 45 m2           |
| Dernier paiement: 01/03  | Gaz: —                 | Parking: 2 places       |
+----------------------------------------------------------------------------+
| HISTORIQUE INTERVENTIONS                                                   |
| 12/02/2026 — Plomberie — Fuite robinet cuisine — Resolu                   |
| 05/11/2025 — Espaces verts — Taille haie — Termine                        |
| 18/06/2025 — Electricite — Panne disjoncteur — Resolu                     |
+============================================================================+""",
        ["GET /api/units/{id}", "GET /api/owners/{id}", "GET /api/incidents?unitId={id}"]))

    # 1.4 Comptabilite
    story.append(screen(
        "1.4 Comptabilite — Saisie ecritures",
        "Saisie d'ecritures comptables SYSCOHADA. Double entree debit/credit. "
        "Ecritures recurrentes programmables. Piece jointe facture.",
        """\
+============================================================================+
| Comptabilite > Journal des ecritures              [+ Nouvelle] [Import XLS]|
+----------------------------------------------------------------------------+
| Filtre: [Journal v] [Periode v] [Copropriete v]           [Rechercher___]  |
+----------------------------------------------------------------------------+
| Date     | Journal | N.piece | Libelle              | Debit     | Credit   |
|----------|---------|---------|----------------------|-----------|----------|
| 01/03/26 | ACH     | FA-0234 | Fact. nettoyage mars | 910 000   |          |
| 01/03/26 | ACH     | FA-0234 | Fournisseur ABC      |           | 910 000  |
| 05/03/26 | BQ      | VIR-045 | Encaiss. V-012 T1    |           | 520 000  |
| 05/03/26 | BQ      | VIR-045 | Copro. M. Kouame     | 520 000   |          |
|----------|---------|---------|----------------------|-----------|----------|
| SOLDE JOURNAL :                                    | 1 430 000 | 1 430 000|
+============================================================================+
| [Saisie rapide]  Date:[__/__/____] Journal:[___v]                          |
| Compte debit: [401000__v]  Montant: [________]                             |
| Compte credit:[512000__v]  Libelle: [________________________]             |
| Piece jointe: [Choisir fichier]           [Valider]  [Annuler]             |
+============================================================================+""",
        ["GET /api/accounting-entries?journal=&period=",
         "POST /api/accounting-entries"]))

    # 1.5 Grand livre
    story.append(screen(
        "1.5 Comptabilite — Grand livre / Balance",
        "Grand livre par compte, balance generale, balance agee. "
        "Export FEC obligatoire pour audit conseil syndical (art. 393 CCH).",
        """\
+============================================================================+
| Comptabilite > Grand Livre              [Balance] [FEC] [Annexes] [Export] |
+----------------------------------------------------------------------------+
| Compte: [401000 — Fournisseurs      v]  Periode: [01/01/2026] a [31/03/26]|
+----------------------------------------------------------------------------+
| Date     | Piece   | Libelle                    | Debit     | Credit    | S|
|----------|---------|----------------------------|-----------|-----------|--|
| 01/01/26 |         | Report a nouveau           |           |           | 0|
| 15/01/26 | FA-0198 | Fact. securite janv.       | 1 040 000 |           |CR|
| 20/01/26 | VIR-012 | Reglement securite         |           | 1 040 000 |0 |
| 01/02/26 | FA-0210 | Fact. espaces verts fev.   | 450 000   |           |CR|
| ...      |         |                            |           |           |  |
|----------|---------|----------------------------|-----------|-----------|--|
| TOTAUX   |         |                            | 3 890 000 | 2 850 000 |CR|
| SOLDE    |         |                            |           | 1 040 000 |  |
+============================================================================+""",
        ["GET /api/accounting-entries?account=401000&from=&to=",
         "GET /api/export/fec"]))

    # 1.6 Appels de fonds
    story.append(screen(
        "1.6 Appels de fonds",
        "Generation automatique des appels de fonds par copropriete. "
        "Repartition par tantiemes, personnalisation calendrier.",
        """\
+============================================================================+
| Appels de fonds > Generation T2 2026                    [+ Generer] [Hist]|
+----------------------------------------------------------------------------+
| Copropriete: [Green City Horizontale v]  Trimestre: [T2 2026 v]            |
+----------------------------------------------------------------------------+
| Lot     | Proprio.        | Tantiemes | Montant      | Statut    | Action |
|---------|-----------------|-----------|--------------|-----------|--------|
| V-001   | M. Koffi        | 450/15000 | 270 000 FCFA | Envoye    | [Voir] |
| V-002   | M. Coulibaly    | 520/15000 | 312 000 FCFA | Paye      | [Voir] |
| A-AC-101| Mme Bamba       | 180/15000 | 108 000 FCFA | En retard | [Rel.] |
| A-AC-102| SCI Ivoire      | 240/15000 | 144 000 FCFA | Envoye    | [Voir] |
| C-01    | Green Shops     | 1200/15000| 720 000 FCFA | Paye      | [Voir] |
|---------|-----------------|-----------|--------------|-----------|--------|
| TOTAL   |                 | 15000     | 9 000 000    | 62% paye  |        |
+============================================================================+""",
        ["GET /api/rent-calls?coOwnershipId=&period=",
         "POST /api/rent-calls/generate"]))

    # 1.7 Impayes
    story.append(screen(
        "1.7 Impayes et relances",
        "Liste des impayes avec anciennete, relances graduees automatiques "
        "(SMS, email, mise en demeure, contentieux). Seuils parametrables.",
        """\
+============================================================================+
| Impayes > Liste                    [Relance groupee] [Mise en dem.] [Exp.] |
+----------------------------------------------------------------------------+
| Filtre: [> 30j v] [Copropriete v] [Montant > ____]                        |
+----------------------------------------------------------------------------+
| Lot     | Proprio.     | Montant du   | Anciennete | Relances | Action     |
|---------|--------------|--------------|------------|----------|------------|
| V-023   | M. Yao       | 1 500 000    | 92 jours   | SMS x2   | [Mise dem]|
| A-BA-305| Mme Diallo   | 648 000      | 65 jours   | SMS x1   | [Relancer]|
| A-CE-201| M. N'Guessan | 324 000      | 31 jours   | Aucune   | [Relancer]|
|---------|--------------|--------------|------------|----------|------------|
| TOTAL IMPAYES : 2 472 000 FCFA  (3 lots / 269)                            |
+============================================================================+""",
        ["GET /api/collections/pending", "POST /api/communications/send-sms"]))

    # 1.8 AG Liste
    story.append(screen(
        "1.8 Assemblees Generales — Liste",
        "Liste des AG planifiees et passees. Preparation ordre du jour, "
        "resolutions types, convocations conformes art. 388 CCH.",
        """\
+============================================================================+
| Assemblees Generales                         [+ Planifier AG] [Historique] |
+----------------------------------------------------------------------------+
| PROCHAINES AG                                                              |
| +----------------------------------------------------------------------+  |
| | AG Ordinaire — Copro Horizontale Green City                          |  |
| | Date: 15/04/2026 14h00 — Club House salle AG                        |  |
| | Statut: Convocations envoyees (380/400)     Quorum: en attente       |  |
| | Resolutions: 12 preparees    Documents: 5 annexes                    |  |
| | [Preparer] [Convoquer] [Animer] [Documents]                          |  |
| +----------------------------------------------------------------------+  |
| | AG Extraordinaire — Copro Verticale Acajou                           |  |
| | Date: 22/04/2026 18h00 — Salle Acajou RDC                           |  |
| | Statut: En preparation       Resolutions: 3 a preparer               |  |
| | [Preparer] [Convoquer]                                               |  |
| +----------------------------------------------------------------------+  |
+============================================================================+""",
        ["GET /api/meetings", "GET /api/meetings/{id}/agenda-items",
         "POST /api/meetings"]))

    # 1.9 AG Animation
    story.append(screen(
        "1.9 AG — Animation et votes en direct",
        "Ecran d'animation AG hybride (presentiel + visio). Calcul quorum "
        "automatique, vote en temps reel, majorites selon regles du syndicat.",
        """\
+============================================================================+
| AG EN COURS — Copro Horizontale Green City              [Pause] [Cloturer]|
+----------------------------------------------------------------------------+
| QUORUM: 67% (268/400 tantiemes)  [OK >= 50%]     Presents: 45  Procur: 23|
+----------------------------------------------------------------------------+
| Resolution n.5 : Approbation budget travaux parking 2026                   |
| Majorite requise: ABSOLUE (art. 25)          Budget: 15 000 000 FCFA      |
+----------------------------------------------------------------------------+
| VOTES EN COURS                    | RESULTATS TEMPS REEL                   |
|                                   |                                        |
| Tantiemes POUR    : 12 450 (72%)  | [==========] 72% POUR                 |
| Tantiemes CONTRE  : 3 200 (19%)   | [===]        19% CONTRE               |
| Tantiemes ABSTENT.: 1 550 (9%)    | [=]           9% ABSTENTION           |
|                                   |                                        |
| Majorite absolue = 8 600          | >> RESOLUTION ADOPTEE                  |
+----------------------------------------------------------------------------+
| [< Res. precedente]  5 / 12  [Res. suivante >]    [Generer PV]            |
+============================================================================+""",
        ["GET /api/meetings/{id}/resolutions", "POST /api/votes",
         "GET /api/votes?resolutionId={id}"]))

    # 1.10 Gestion locative
    story.append(screen(
        "1.10 Gestion locative — Baux residentiels",
        "Liste des baux CCH (art. 414-450). Quittancement automatique, "
        "revision triennale (art. 423), caution max 2 mois (art. 416).",
        """\
+============================================================================+
| Gestion locative > Baux residentiels           [+ Nouveau bail] [Export]   |
+----------------------------------------------------------------------------+
| Filtre: [Statut v] [Batiment v] [Echeance < __j]                          |
+----------------------------------------------------------------------------+
| Lot      | Locataire      | Loyer     | Debut    | Fin      | Statut      |
|----------|----------------|-----------|----------|----------|-------------|
| A-AC-101 | M. Dje         | 420 000   | 01/01/24 | 31/12/26 | Actif       |
| A-AC-305 | Mme Toure      | 540 000   | 01/06/23 | 31/05/26 | Ech. < 90j  |
| A-BA-102 | M. Konate      | 420 000   | 01/09/24 | 31/08/27 | Actif       |
| V-045    | SCI Palm       | 850 000   | 01/03/25 | 28/02/28 | Actif       |
|----------|----------------|-----------|----------|----------|-------------|
| [Quittancer mois] [Revision triennale] [Regularisation annuelle]           |
+============================================================================+""",
        ["GET /api/leases?type=residential", "POST /api/rent-receipts/generate"]))

    # 1.11 Baux commerciaux
    story.append(screen(
        "1.11 Baux commerciaux OHADA",
        "Baux AUDCG art. 101-134. Duree libre, revision triennale, "
        "droit au renouvellement apres 3 ans. Enseignes COSMOS.",
        """\
+============================================================================+
| Gestion locative > Baux commerciaux OHADA               [+ Nouveau] [Exp] |
+----------------------------------------------------------------------------+
| Lot      | Enseigne         | Loyer/mois  | Debut    | Revision | Statut  |
|----------|------------------|-------------|----------|----------|---------|
| C-01     | Super U          | 5 925 000   | 01/01/23 | 01/01/26 | Rev.due |
| C-02     | KIABI            | 2 100 000   | 01/06/23 | 01/06/26 | Actif   |
| C-03     | City Sports      | 1 800 000   | 01/03/24 | 01/03/27 | Actif   |
| RP-MU1   | KFC              | 3 200 000   | 01/09/23 | 01/09/26 | Actif   |
| ORYX     | Oryx Energies    | 4 500 000   | 01/01/22 | Concession| Actif  |
|----------|------------------|-------------|----------|----------|---------|
| [Revision OHADA] [Renouvellement] [Indice ILC]                             |
+============================================================================+""",
        ["GET /api/leases?type=commercial",
         "GET /api/lease-revisions?type=OHADA"]))

    # 1.12 Candidatures
    story.append(screen(
        "1.12 Candidatures locataires + Scoring",
        "Gestion des candidatures avec scoring automatique. "
        "Dossier complet, verification pieces, notation multicritere.",
        """\
+============================================================================+
| Candidatures > Lot A-BA-201 (F3, 97m2, 420 000 FCFA/mois)    [+ Ajouter] |
+----------------------------------------------------------------------------+
| Candidat         | Revenus     | Score  | Docs   | Statut         | Act.  |
|------------------|-------------|--------|--------|----------------|-------|
| M. Bah Ibrahim   | 1 400 000   | 85/100 | 5/5    | En cours       | [Voir]|
| Mme Ouattara     | 1 100 000   | 72/100 | 4/5    | Docs manquants | [Voir]|
| M. Zadi          | 900 000     | 58/100 | 5/5    | Insuffisant    | [Voir]|
|------------------|-------------|--------|--------|----------------|-------|
| Criteres: Revenus >= 3x loyer | Garant | Emploi stable | Historique      |
| [Approuver candidat] [Refuser] [Generer bail]                              |
+============================================================================+""",
        ["GET /api/tenant-applications?unitId={id}",
         "POST /api/tenant-applications"]))

    # 1.13 Travaux et incidents
    story.append(screen(
        "1.13 Travaux et incidents",
        "Suivi complet : incidents signales, ordres de service, "
        "mise en concurrence prestataires, budget travaux, PPT.",
        """\
+============================================================================+
| Travaux > Incidents actifs                    [+ Creer OS] [Budget] [PPT] |
+----------------------------------------------------------------------------+
| Filtre: [Priorite v] [Categorie v] [Statut v] [Copropriete v]             |
+----------------------------------------------------------------------------+
| #    | Lot     | Categorie   | Description          | Priorite | Statut   |
|------|---------|-------------|----------------------|----------|----------|
| I-47 | V-012   | Plomberie   | Fuite eau cuisine    | CRITIQUE | EnCours  |
| I-46 | Commun  | Ascenseur   | Panne asc. Acajou    | HAUTE    | OS emis  |
| I-45 | A-CE-3  | Electricite | Eclairage palier 3   | MOYENNE  | Signale  |
| I-44 | STEP    | STEP        | Pompe relevage HS    | HAUTE    | Resolu   |
|------|---------|-------------|----------------------|----------|----------|
| STATS: 7 ouverts | 2 critiques | 12 resolus ce mois | Cout: 2.8M FCFA   |
+============================================================================+""",
        ["GET /api/incidents?status=open", "GET /api/incidents/stats",
         "GET /api/work-orders"]))

    # 1.14 Ordres de service
    story.append(screen(
        "1.14 Ordres de service — Detail",
        "Workflow complet : Brouillon > Approuve > En cours > Termine > "
        "Facture > Paye. Mise en concurrence avec tableau comparatif.",
        """\
+============================================================================+
| OS-0089 — Reparation pompe STEP                    [Approuver] [Annuler]  |
+----------------------------------------------------------------------------+
| Incident: I-44          | Lot: STEP              | Priorite: HAUTE        |
| Categorie: STEP         | Cout estime: 1 200 000 | Statut: EN COURS       |
+----------------------------------------------------------------------------+
| DEVIS RECUS (mise en concurrence)                                          |
| Prestataire          | Montant      | Delai   | Note | Statut              |
| ABC Maintenance      | 1 150 000    | 3 jours | 4/5  | >> RETENU           |
| XYZ Plomberie        | 1 400 000    | 5 jours | 3/5  | Refuse              |
| EcoService CI        | 980 000      | 7 jours | 4/5  | En attente          |
+----------------------------------------------------------------------------+
| SUIVI INTERVENTION                                                         |
| 18/03 09:00 — Diagnostic sur site (photo jointe)                           |
| 19/03 08:00 — Commande piece (pompe Grundfos SP-30)                        |
| 20/03 14:00 — Installation en cours                                        |
| [Ajouter suivi] [Cloturer] [Facturer: _______ FCFA]                       |
+============================================================================+""",
        ["GET /api/work-orders/{id}", "PUT /api/work-orders/{id}/approve",
         "PUT /api/work-orders/{id}/complete"]))

    # 1.15 Communication
    story.append(screen(
        "1.15 Communication multicanal",
        "Centre de communication : email, SMS, courrier postal, LRAR, "
        "notification push. Publipostage personnalise avec modeles.",
        """\
+============================================================================+
| Communication > Nouvelle diffusion             [Modeles] [Historique]      |
+----------------------------------------------------------------------------+
| Type: [o] Email  [o] SMS  [o] Push  [o] Courrier  [o] LRAR                |
| Destinataires: [Tous proprios v] [Copro Horizontale v]  (380 dest.)        |
+----------------------------------------------------------------------------+
| Objet: [Convocation AG Ordinaire du 15 avril 2026__________________]       |
|                                                                            |
| +--- EDITEUR -----------------------------------------------------------+ |
| | Madame, Monsieur,                                                     | |
| |                                                                       | |
| | Nous avons l'honneur de vous convoquer a l'Assemblee Generale         | |
| | Ordinaire de la copropriete Green City Bassam, qui se tiendra le :    | |
| | {{date_ag}} a {{heure_ag}} au {{lieu_ag}}.                            | |
| |                                                                       | |
| | Veuillez trouver ci-joint l'ordre du jour et les documents annexes.   | |
| +-----------------------------------------------------------------------+ |
| Pieces jointes: [OdJ_AG_2026.pdf] [Budget_2026.pdf] [+ Ajouter]           |
| [Apercu] [Envoyer maintenant] [Programmer: __/__/____ __:__]               |
+============================================================================+""",
        ["POST /api/broadcasts", "GET /api/message-templates",
         "POST /api/communication-messages"]))

    # 1.16 GED
    story.append(screen(
        "1.16 GED — Gestion Electronique des Documents",
        "Upload, indexation, recherche full-text, classement arborescent. "
        "Categories : bail, facture, PV AG, assurance, plan, reglement...",
        """\
+============================================================================+
| Documents > GED                            [+ Upload] [Recherche avancee] |
+----------------------------------------------------------------------------+
| Recherche: [_________________________]  Categorie: [Toutes v]              |
+----------------------------------------------------------------------------+
| Arborescence          | Documents                                          |
| v Green City Horiz.   | Nom                  | Cat.     | Date    | Taille |
|   v AG                | PV_AG_2025.pdf       | PV AG    | 15/12/25| 2.4 MB |
|   v Contrats          | PV_AG_2024.pdf       | PV AG    | 18/11/24| 1.8 MB |
|   v Assurances        | Reglement_copro.pdf  | Reglement| 01/01/24| 450 KB |
| v Acajou              | Budget_2026.pdf      | Finance  | 01/01/26| 320 KB |
|   v Travaux           | Contrat_securite.pdf | Contrat  | 01/06/25| 1.1 MB |
|   v Baux              |                                                    |
| v COSMOS              | [Telecharger] [Apercu] [Supprimer] [Deplacer]      |
+============================================================================+""",
        ["GET /api/documents?category=&search=",
         "POST /api/documents (multipart/form-data)"]))

    # 1.17 Reporting
    story.append(screen(
        "1.17 Reporting / KPI (50+ indicateurs)",
        "Tableaux de bord avances avec graphiques interactifs Chart.js. "
        "Drill-down, comparatifs multi-coproprietes, export Excel/PDF.",
        """\
+============================================================================+
| Reporting > Tableau de bord analytique          [Export PDF] [Export Excel] |
+----------------------------------------------------------------------------+
| Copropriete: [Toutes (consolide) v]  Periode: [Annee 2026 v]              |
+----------------------------------------------------------------------------+
| +--- FINANCIER --------+  +--- OCCUPATION --------+  +--- TRAVAUX ------+ |
| | Tresorerie: 45.2M    |  | Taux global: 94%      |  | Budget: 15M      | |
| | Budget: 108M/an      |  | Villas: 98%            |  | Consomme: 8.2M   | |
| | Realise: 52%         |  | Appts: 92%             |  | OS ouverts: 4    | |
| | [graphe camembert]   |  | COSMOS: 95%            |  | [graphe barres]  | |
| +----------------------+  +------------------------+  +------------------+ |
|                                                                            |
| +--- EVOLUTION TRESORERIE 12 MOIS (Chart.js line) ----------------------+ |
| |  45M |          *                                                      | |
| |  40M |      *       *   *                                              | |
| |  35M |  *               *   *   *       *                              | |
| |  30M |*                          *   *       *   *                     | |
| |      | Jan  Fev  Mar  Avr  Mai  Jun  Jul  Aou  Sep  Oct  Nov  Dec      | |
| +-----------------------------------------------------------------------+ |
+============================================================================+""",
        ["GET /api/dashboard/kpis", "GET /api/payments/summary?period=yearly"]))

    # 1.18 Parametrage general
    story.append(screen(
        "1.18 Parametrage general",
        "Tous les taux, montants, seuils et pourcentages. "
        "Aucune valeur en dur. Historique des modifications avec audit.",
        """\
+============================================================================+
| Parametrage > Parametres financiers            [Historique] [Restaurer]    |
+----------------------------------------------------------------------------+
| ONGLETS: [Financier] [Charges] [Baux] [Alertes] [Systeme] [Pays]          |
+----------------------------------------------------------------------------+
| Parametre                              | Valeur actuelle | Legal     |Act.|
|----------------------------------------|-----------------|-----------|-----|
| Taux gestion locative                  | [6___] % TTC    |           | [M] |
| Remuneration syndic copro horiz.       | [20__] %        | Max 30%   | [M] |
| Remuneration syndic copro vert.        | [20__] %        | Max 30%   | [M] |
| Frais enregistrement immobilier        | [8.3_] %        |           | [M] |
| Frais commercialisation                | [6___] % TTC    |           | [M] |
| Frais de dossier fixes                 | [500 000] FCFA  |           | [M] |
| Taux TVA standard                      | [18__] %        |           | [M] |
| Penalites retard impayes              | [1.5_] %/mois   |           | [M] |
| Remise paiement comptant              | [5___] %        |           | [M] |
| Caution max bail habitation           | [2___] mois     | Art.416   | [M] |
|----------------------------------------|-----------------|-----------|-----|
| [M] = Modifier  |  Derniere modif: Admin, 15/03/2026, "Maj TVA"          |
+============================================================================+""",
        ["GET /api/organizations/{id}/settings",
         "PUT /api/organizations/{id}/settings"]))

    # 1.19 Parametrage structure
    story.append(screen(
        "1.19 Parametrage structure immobiliere",
        "Configuration des coproprietes, batiments, types d'entites. "
        "100% generique et revendable. Tout est parametre, pas du code.",
        """\
+============================================================================+
| Parametrage > Structure immobiliere                                        |
+----------------------------------------------------------------------------+
| ONGLETS: [Coproprietes] [Batiments] [Types entites] [Cles repartition]     |
+----------------------------------------------------------------------------+
| COPROPRIETES                                            [+ Ajouter]        |
| Nom                          | Niveau      | Lots | Budget/mois | Act.    |
|------------------------------|-------------|------|-------------|---------|
| Green City Bassam            | Horizontale | 269  | 9 015 600   | [Modif] |
| Copro Verticale Acajou       | Verticale   | 40   | 1 000 000   | [Modif] |
| Copro Verticale Baobab       | Verticale   | 40   | 1 000 000   | [Modif] |
| Copro Verticale Cedre        | Verticale   | 40   | 1 000 000   | [Modif] |
| COSMOS Commercial            | Horizontale | 18   | 3 500 000   | [Modif] |
|------------------------------|-------------|------|-------------|---------|
| TYPES D'ENTITES (liste dynamique)                       [+ Ajouter]        |
| Villa Minan F4 | Villa Asue F5 | Villa Boya F6 | Appt F3 | Appt F4 |     |
| Commerce | Cinema | Restaurant | Station-service | Clinique | Ecole | ... |
+============================================================================+""",
        ["GET /api/co-ownerships", "GET /api/buildings",
         "POST /api/co-ownerships", "POST /api/buildings"]))

    # 1.20 Gestion utilisateurs
    story.append(screen(
        "1.20 Gestion utilisateurs / Roles / Droits",
        "19 profils precables. Matrice de droits granulaire (module x action). "
        "Perimetre de donnees par utilisateur. 2FA pour admin/comptable/DAF.",
        """\
+============================================================================+
| Parametrage > Utilisateurs et roles                   [+ Utilisateur]      |
+----------------------------------------------------------------------------+
| ONGLETS: [Utilisateurs] [Roles] [Matrice droits] [Journal audit]           |
+----------------------------------------------------------------------------+
| Nom              | Email              | Role(s)            | Copro   |Stat.|
|------------------|--------------------|--------------------|---------|----|
| Admin Systeme    | admin@gs.ci        | Administrateur     | Toutes  | OK |
| M. Diop          | diop@isis.ci       | Gestionnaire copro | Acajou+ | OK |
| Mme Kone         | kone@isis.ci       | Comptable          | Toutes  | OK |
| M. Traore        | traore@isis.ci     | Gest. terrain (mob)| Toutes  | OK |
| M. Kouame J-M    | jm@mail.ci         | President CS       | Horiz.  | OK |
| Mme Bamba         | bamba@mail.ci       | Coproprietaire     | Acajou  | OK |
|------------------|--------------------|--------------------|---------|----|
| [Modifier] [Suspendre] [Desactiver] [Historique connexions]                |
+============================================================================+""",
        ["GET /api/auth/users", "POST /api/auth/register",
         "PUT /api/auth/users/{id}/roles"]))

    # 1.21 Veille juridique
    story.append(screen(
        "1.21 Veille juridique",
        "Surveillance JORCI, OHADA, ARTCI, DGI. Classification automatique, "
        "alertes proactives, suggestions de mise a jour des parametres.",
        """\
+============================================================================+
| Veille juridique > Derniers textes               [Config sources] [Rech.] |
+----------------------------------------------------------------------------+
| Filtre: [Domaine v] [Criticite v] [Statut v]                              |
+----------------------------------------------------------------------------+
| Date     | Source  | Domaine      | Resume                    | Crit. |St. |
|----------|---------|--------------|---------------------------|-------|----|
| 15/03/26 | JORCI   | Copropriete  | Modif art. 397 CCH —      | URGENT| NEW|
|          |         |              | plafond syndic a 25%      |       |    |
| 10/03/26 | DGI     | Fiscalite    | Nouveau bareme impot fonc.| ATTEN.| LU |
| 01/03/26 | OHADA   | Bail comm.   | Jurisprudence AUDCG 116   | INFO  | LU |
| 20/02/26 | ARTCI   | Donnees pers.| Nouveau decret ARTCI 2026 | ATTEN.| OK |
|----------|---------|--------------|---------------------------|-------|----|
| >> ALERTE : Le plafond art. 397 est passe de 30% a 25%.                   |
|    Parametre actuel : 20% — CONFORME. Mettre a jour le max legal ?         |
|    [Mettre a jour parametre] [Ignorer] [Voir texte source]                 |
+============================================================================+""",
        ["GET /api/legal-references?domain=&search=",
         "POST /api/legal-references"]))

    # 1.22 Export/Import
    story.append(screen(
        "1.22 Export / Import Excel",
        "Chaque liste dispose de [Export Excel] et [Import Excel]. "
        "Modeles telechargeables, validation pre-import, mode simulation.",
        """\
+============================================================================+
| Export / Import > Centre de donnees                                        |
+----------------------------------------------------------------------------+
| MODULE                  | EXPORT              | IMPORT                     |
|-------------------------|---------------------|----------------------------|
| Lots / Patrimoine       | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
| Coproprietaires         | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
| Locataires              | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
| Comptabilite (FEC)      | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
| Appels de fonds         | [Exporter .xlsx]     | [Importer]                 |
| Charges                 | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
| Baux commerciaux        | [Exporter .xlsx]     | [Importer]                 |
| Incidents / Travaux     | [Exporter .xlsx]     | [Importer]                 |
| Prestataires            | [Exporter .xlsx]     | [Importer] [Modele .xlsx]  |
|-------------------------|---------------------|----------------------------|
| Dernier import: 15/03 — Lots (269 lignes, 0 erreurs, 269 crees)           |
| [Voir journal d'import]                                                    |
+============================================================================+""",
        ["GET /api/export/{type}?format=csv",
         "POST /api/import/{type} (multipart)"]))

    # 1.23 Annuaire
    story.append(screen(
        "1.23 Annuaire centralise",
        "Annuaire unique filtrable : residents, prestataires, equipe syndic. "
        "Organigramme, evaluation fournisseurs, conformite Loi 2013-450.",
        """\
+============================================================================+
| Annuaire > Tous les contacts                    [+ Ajouter] [Export CSV]   |
+----------------------------------------------------------------------------+
| Filtre: [Type v] [Copropriete v] [Immeuble v]    Recherche: [___________]  |
+----------------------------------------------------------------------------+
| ONGLETS: [Residents] [Prestataires] [Equipe syndic]                        |
+----------------------------------------------------------------------------+
| Nom              | Type         | Lot/Societe     | Tel           | Email  |
|------------------|--------------|-----------------|---------------|--------|
| M. Kouame J-M    | Proprio occ. | V-012           | +225 07 XX XX | jm@... |
| Mme Bamba A.     | Proprio bail.| A-AC-101        | +225 05 XX XX | ab@... |
| M. Dje K.        | Locataire    | A-AC-101        | +225 01 XX XX | dk@... |
| ABC Maintenance  | Prestataire  | Plomberie       | +225 27 XX XX | abc@.. |
| ISIS Immobilier  | Syndic       | Gestionnaire    | +225 27 XX XX | isis@. |
|------------------|--------------|-----------------|---------------|--------|
+============================================================================+""",
        ["GET /api/owners", "GET /api/suppliers",
         "GET /api/lease-tenants"]))

    story.append(PageBreak())

    # =========================================================
    # 2. MOBSYNDIC
    # =========================================================
    story.append(Paragraph("2. MobSyndic — PWA Syndic Terrain (:5052)", styles["AppTitle"]))
    story.append(Paragraph(
        "Application mobile terrain pour le gestionnaire syndic. "
        "5-8 KPI synthetiques, incidents avec photo+GPS, VTI checklist, "
        "impayes avec relance SMS, validation factures en un tap. "
        "PAS de parametrage, PAS de comptabilite detaillee.",
        styles["SectionIntro"]))

    # 2.1 Dashboard
    story.append(screen(
        "2.1 Dashboard KPI terrain",
        "5 cartes KPI empilees avec sparklines. Actions rapides terrain (2x2). "
        "Alertes urgentes en bas.",
        """\
+----------------------------------+
| [GreenSyndic Syndic]   [Deconn.] |
+----------------------------------+
|                                  |
| +------------------------------+ |
| | IMPAYES        4 250 000 FCFA| |
| | [^] +3%        [sparkline]   | |
| +------------------------------+ |
| | TAUX OCCUP.    94%           | |
| |                [sparkline]   | |
| +------------------------------+ |
| | INCIDENTS      7 (2 urgents) | |
| | [^] +2         [sparkline]   | |
| +------------------------------+ |
| | TRESORERIE     12 500 000    | |
| |                [sparkline]   | |
| +------------------------------+ |
|                                  |
| +--------+  +---------+         |
| |Incident|  |Scanner  |         |
| |+ Photo |  |Facture  |         |
| +--------+  +---------+         |
| +--------+  +---------+         |
| |Lancer  |  |Envoyer  |         |
| |  VTI   |  |  SMS    |         |
| +--------+  +---------+         |
|                                  |
| ALERTES                          |
| ! Impaye >3mois V-023 1.5M      |
| ! Bail R4 expire 30j            |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["GET /api/dashboard/kpis"]))

    # 2.2 Creer incident
    story.append(screen(
        "2.2 Creer incident (photo + GPS)",
        "Formulaire terrain : photo appareil, geolocalisation auto, "
        "categorie, priorite, description. Creation immediate d'un OS.",
        """\
+----------------------------------+
| < Retour   Nouvel incident       |
+----------------------------------+
|                                  |
| +------------------------------+ |
| |                              | |
| |    [  PHOTO APPAREIL  ]      | |
| |    [  Prendre / Galerie]     | |
| |                              | |
| +------------------------------+ |
|                                  |
| Lot concern :                   |
| [V-012 — Villa Asue ________v] |
|                                  |
| Categorie:                       |
| [Plomberie___________________v] |
|                                  |
| Priorite:                        |
| [o] Basse [o] Moyenne           |
| [x] Haute [o] Critique          |
|                                  |
| Description:                     |
| +------------------------------+ |
| | Fuite eau importante sous    | |
| | evier cuisine. Degat des     | |
| | eaux en cours.               | |
| +------------------------------+ |
|                                  |
| GPS: 5.3168, -3.7358 [auto]     |
|                                  |
| [ Creer incident + OS ]         |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["POST /api/incidents",
         "POST /api/documents (photo)",
         "POST /api/work-orders"]))

    # 2.3 VTI
    story.append(screen(
        "2.3 VTI — Visite Technique d'Immeuble",
        "Checklist interactive par zone. Photos geolocalisees, notes, "
        "generation automatique du rapport PDF.",
        """\
+----------------------------------+
| < Retour   VTI Imm. Acajou      |
+----------------------------------+
| Date: 21/03/2026   Etage: [3 v] |
+----------------------------------+
|                                  |
| CHECKLIST ETAGE 3                |
| [x] Eclairage palier        [OK]|
| [x] Etat peinture murs      [OK]|
| [ ] Porte coupe-feu        [NOK]|
|     > Note: Ferme-porte HS      |
|     > [Photo jointe]            |
| [x] Extincteur present      [OK]|
| [ ] Propr. sol              [NOK]|
|     > Note: Tache humidite      |
|     > [Photo jointe]            |
| [x] Boite aux lettres       [OK]|
| [x] Interphone              [OK]|
|                                  |
| Progression: 5/7 zones OK       |
| [===== barre ========]  71%     |
|                                  |
| [+ Ajouter observation]         |
| [Enregistrer] [Generer rapport]  |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["POST /api/incidents (type=VTI)",
         "POST /api/documents"]))

    # 2.4 Impayes
    story.append(screen(
        "2.4 Liste des impayes",
        "Tri par montant/anciennete. Action rapide : relance SMS en un tap. "
        "Pas de tableau complexe, format carte mobile.",
        """\
+----------------------------------+
| < Retour   Impayes              |
+----------------------------------+
| Tri: [Anciennete v] Total: 2.4M |
+----------------------------------+
|                                  |
| +------------------------------+ |
| | V-023 — M. Yao              | |
| | 1 500 000 FCFA | 92 jours   | |
| | Relances: SMS x2             | |
| | [Appeler] [SMS] [Mise dem.]  | |
| +------------------------------+ |
|                                  |
| +------------------------------+ |
| | A-BA-305 — Mme Diallo       | |
| | 648 000 FCFA   | 65 jours   | |
| | Relances: SMS x1             | |
| | [Appeler]  [SMS]             | |
| +------------------------------+ |
|                                  |
| +------------------------------+ |
| | A-CE-201 — M. N'Guessan     | |
| | 324 000 FCFA   | 31 jours   | |
| | Relances: Aucune             | |
| | [Appeler]  [SMS]             | |
| +------------------------------+ |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["GET /api/collections/pending"]))

    # 2.5 Validation factures
    story.append(screen(
        "2.5 Validation factures",
        "Factures pre-saisies par OCR ou desktop. Le terrain valide "
        "ou refuse en un tap avec commentaire optionnel.",
        """\
+----------------------------------+
| < Retour   Validation            |
+----------------------------------+
| 3 factures en attente            |
+----------------------------------+
|                                  |
| +------------------------------+ |
| | FA-0234 — ABC Maintenance    | |
| | Nettoyage mars 2026          | |
| | Montant: 910 000 FCFA        | |
| | OS: OS-0085                  | |
| | [Voir PDF]                   | |
| |                              | |
| | [VALIDER]     [REFUSER]      | |
| +------------------------------+ |
|                                  |
| +------------------------------+ |
| | FA-0235 — Oryx Energies      | |
| | Redevance T1 2026            | |
| | Montant: 4 500 000 FCFA      | |
| | [Voir PDF]                   | |
| |                              | |
| | [VALIDER]     [REFUSER]      | |
| +------------------------------+ |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["GET /api/work-orders?status=Completed",
         "PUT /api/work-orders/{id}/invoice"]))

    # 2.6 - 2.9 plus courts
    story.append(screen(
        "2.6 Fiche lot resumee",
        "Consultation rapide : type, surface, proprietaire, locataire, solde, "
        "dernier evenement. Pas de modification depuis mobile.",
        """\
+----------------------------------+
| < Retour   Lot V-012             |
+----------------------------------+
| Villa F5 Asue — 223 m2           |
| Proprietaire: M. Kouame J-M     |
| Locataire: —  (occupe proprio)   |
| Solde charges: 0 FCFA            |
| Copro H: Green City Bassam      |
+----------------------------------+
| DERNIERS EVENEMENTS              |
| 12/02 Plomberie — Resolu        |
| 05/11 Espaces verts — Termine   |
+----------------------------------+
| [Appeler] [Creer incident]       |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["GET /api/units/{id}"]))

    story.append(screen(
        "2.7 Etat des lieux mobile",
        "Checklist piece par piece, photos horodatees, signature tactile. "
        "Conformite art. 427 CCH.",
        """\
+----------------------------------+
| < Retour   Etat des lieux       |
+----------------------------------+
| Lot: A-AC-101 | Type: ENTREE    |
| Locataire: M. Dje               |
+----------------------------------+
| PIECE: Salon                     |
| Murs:    [Bon v] [Photo]        |
| Sol:     [Bon v] [Photo]        |
| Plafond: [Bon v]                |
| Fenetres:[Usure v] [Photo]      |
|   Note: Rayure vitre gauche     |
| Prises:  [Bon v]                |
| Eclairage:[Bon v]               |
+----------------------------------+
| PIECE: Cuisine  [suivante >]    |
+----------------------------------+
| Progression: 2/6 pieces         |
| [Signer] [Generer PDF]          |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["POST /api/documents (type=etat-lieux)"]))

    story.append(screen(
        "2.8 Scan facture OCR",
        "Prise de photo de facture. Envoi au serveur pour traitement OCR. "
        "Pre-remplissage automatique des champs.",
        """\
+----------------------------------+
| < Retour   Scanner facture       |
+----------------------------------+
|                                  |
| +------------------------------+ |
| |                              | |
| |   [  CAMERA DOCUMENT  ]     | |
| |                              | |
| |   Cadrez la facture          | |
| |   dans le rectangle          | |
| |                              | |
| +------------------------------+ |
|                                  |
| [Prendre photo]   [Galerie]     |
|                                  |
| -- APRES SCAN (pre-rempli) --   |
| Fournisseur: [ABC Maint.____]   |
| N. facture:  [FA-0236________]  |
| Montant:     [1 150 000______]  |
| Date:        [20/03/2026_____]  |
|                                  |
| [Envoyer au desktop]             |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["POST /api/documents (type=invoice-scan)"]))

    story.append(screen(
        "2.9 Notifications push",
        "Centre de notifications : alertes impayes critiques, incidents urgents, "
        "rappels AG, validations en attente.",
        """\
+----------------------------------+
| < Retour   Notifications    (5) |
+----------------------------------+
|                                  |
| AUJOURD'HUI                      |
| [!] Incident CRITIQUE V-012     |
|     Fuite eau — 14:23           |
|                                  |
| [!] Impaye >90j V-023           |
|     1 500 000 FCFA — 10:00      |
|                                  |
| HIER                             |
| [i] Facture validee FA-0233     |
|     ABC Maintenance — 16:45     |
|                                  |
| [i] Nouveau locataire A-BA-102  |
|     M. Konate — bail signe      |
|                                  |
| [i] Rappel: AG dans 25 jours    |
|     Copro Horizontale 15/04     |
|                                  |
+----------------------------------+
| [KPI] [Incid] [VTI] [Imp] [Val] |
+----------------------------------+""",
        ["GET /api/notifications"]))

    story.append(PageBreak())

    # =========================================================
    # 3. MOBPROPRIO
    # =========================================================
    story.append(Paragraph("3. MobProprio — PWA Proprietaire (:5053)", styles["AppTitle"]))
    story.append(Paragraph(
        "Application mobile pour les ~400 proprietaires. Consultation solde, "
        "paiement charges par mobile money, vote AG, incidents, documents, "
        "messagerie. Reconnaissance auto : coproprietaire / bailleur / CS.",
        styles["SectionIntro"]))

    story.append(screen(
        "3.1 Accueil",
        "Page d'accueil avec salutation, solde, bouton payer, actions rapides "
        "(6 boutons), flux d'actualites de la copropriete.",
        """\
+----------------------------------+
| [Avatar] Bonjour Jean-Marc  [3] |
+----------------------------------+
|                                  |
| +------------------------------+ |
| | SOLDE ACTUEL                 | |
| | 0 FCFA                      | |
| | [  Payer mes charges  ]      | |
| +------------------------------+ |
|                                  |
| +------+  +------+  +------+   |
| | Mes  |  | Mon  |  |Signal|   |
| | Docs |  |Compte|  |Incid.|   |
| +------+  +------+  +------+   |
| +------+  +------+  +------+   |
| |Voter |  |Messa-|  | Mon  |   |
| | AG   |  |gerie |  |Imm.  |   |
| +------+  +------+  +------+   |
|                                  |
| ACTUALITES                       |
| PV AG 2025 disponible [Consul.] |
| Travaux parking 15/04 [Details] |
| Appel de fonds T2 2026 [Payer]  |
|                                  |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/dashboard/kpis (owner scope)",
         "GET /api/notifications"]))

    story.append(screen(
        "3.2 Mon compte — Historique",
        "Historique des appels de fonds et paiements. Solde detaille "
        "par copropriete (horizontale + verticale si applicable).",
        """\
+----------------------------------+
| < Retour   Mon compte           |
+----------------------------------+
| SOLDE GLOBAL : 0 FCFA            |
|                                  |
| Copro Horizontale: 0 FCFA       |
| Copro Vert. Acajou: N/A         |
+----------------------------------+
| HISTORIQUE                       |
| 01/03/26 Appel T1 Horiz.        |
|          -520 000 FCFA           |
| 05/03/26 Paiement Orange Money  |
|          +520 000 FCFA           |
| 01/12/25 Appel T4 Horiz.        |
|          -520 000 FCFA           |
| 10/12/25 Paiement virement      |
|          +520 000 FCFA           |
+----------------------------------+
| [Telecharger releve PDF]         |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/payments?ownerId={id}",
         "GET /api/rent-calls?ownerId={id}"]))

    story.append(screen(
        "3.3 Payer mes charges",
        "Paiement par Orange Money, MTN Money, Wave ou virement. "
        "Integration CinetPay. Confirmation par SMS.",
        """\
+----------------------------------+
| < Retour   Payer                |
+----------------------------------+
| Montant du : 520 000 FCFA       |
| Copropriete: Green City Horiz.  |
| Periode: T2 2026                |
+----------------------------------+
|                                  |
| MOYEN DE PAIEMENT               |
|                                  |
| [x] Orange Money                |
| [ ] MTN Money                   |
| [ ] Wave                        |
| [ ] Virement bancaire           |
|                                  |
| Numero: [+225 07 __ __ __ __]   |
|                                  |
| +------------------------------+ |
| | Vous allez payer             | |
| | 520 000 FCFA                 | |
| | via Orange Money             | |
| | Frais: 0 FCFA                | |
| +------------------------------+ |
|                                  |
| [  Confirmer le paiement  ]     |
|                                  |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["POST /api/cinetpay/initialize",
         "POST /api/payments"]))

    story.append(screen(
        "3.4 Signaler un incident",
        "Meme formulaire que MobSyndic mais pour le proprietaire/resident. "
        "Photo, geolocalisation, description. Suivi en temps reel.",
        """\
+----------------------------------+
| < Retour   Signaler             |
+----------------------------------+
| +------------------------------+ |
| |   [  PRENDRE PHOTO  ]       | |
| +------------------------------+ |
|                                  |
| Mon lot: V-012 (auto-detecte)   |
|                                  |
| Zone:                            |
| [x] Parties privatives          |
| [ ] Parties communes            |
|                                  |
| Type de probleme:                |
| [Fuite d'eau_______________v]   |
|                                  |
| Description:                     |
| [Fuite sous evier cuisine,     ]|
| [eau coule en continu.         ]|
|                                  |
| GPS: 5.3168, -3.7358            |
|                                  |
| [ Envoyer ]                      |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["POST /api/incidents"]))

    story.append(screen(
        "3.5 Mes documents",
        "Acces aux PV d'AG, reglement de copropriete, carnet d'entretien, "
        "quittances, budget. Telechargement PDF.",
        """\
+----------------------------------+
| < Retour   Documents            |
+----------------------------------+
| Recherche: [________________]   |
+----------------------------------+
| PV ASSEMBLEES GENERALES          |
| PV AG Ordinaire 2025     [PDF]  |
| PV AG Extra. nov 2025    [PDF]  |
|                                  |
| REGLEMENT & STATUTS              |
| Reglement copropriete    [PDF]  |
| Code bon voisinage       [PDF]  |
|                                  |
| FINANCES                         |
| Budget previsionnel 2026 [PDF]  |
| Repartition charges T1  [PDF]  |
|                                  |
| CARNET D'ENTRETIEN               |
| Derniere maj: 15/03/2026        |
| [Consulter]                      |
|                                  |
| MES QUITTANCES                   |
| Quittance mars 2026      [PDF]  |
| Quittance fev 2026       [PDF]  |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/documents?ownerId={id}"]))

    story.append(screen(
        "3.6 Messagerie",
        "Communication directe avec le syndic et le conseil syndical. "
        "Historique des echanges, pieces jointes.",
        """\
+----------------------------------+
| < Retour   Messages             |
+----------------------------------+
| CONVERSATIONS                    |
|                                  |
| +------------------------------+ |
| | Syndic ISIS Immobilier       | |
| | Votre demande a ete prise   | |
| | en compte.        Hier 16:30| |
| +------------------------------+ |
|                                  |
| +------------------------------+ |
| | Conseil Syndical             | |
| | Reunion CS prevue le 10/04  | |
| |                    12/03 09h | |
| +------------------------------+ |
|                                  |
| --- Conversation ouverte ---     |
| [Syndic] 16:30                   |
| Votre demande concernant la      |
| fuite a ete transmise a ABC      |
| Maintenance. Intervention        |
| prevue demain matin.             |
|                                  |
| [Vous] 14:23                     |
| Bonjour, j'ai signale une fuite  |
| ce matin. Quand est prevue       |
| l'intervention ?                 |
|                                  |
| [Message...________] [Envoyer]   |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/communication-messages?userId={id}",
         "POST /api/communication-messages"]))

    story.append(screen(
        "3.7 Voter AG a distance",
        "Vote en temps reel depuis le mobile. Pouvoir de delegation. "
        "Resultats en direct. Visioconference integree.",
        """\
+----------------------------------+
| < Retour   AG en cours          |
+----------------------------------+
| AG Ordinaire — Green City Horiz.|
| 15/04/2026 14h00                |
| Quorum: 67% [OK]                |
+----------------------------------+
|                                  |
| RESOLUTION N.5                   |
| Approbation budget travaux       |
| parking 2026 — 15 000 000 FCFA  |
| Majorite requise: ABSOLUE        |
|                                  |
| MON VOTE:                        |
| [  POUR  ] [CONTRE] [ABSTENTION]|
|                                  |
| Resultats provisoires:           |
| POUR: 72%  CONTRE: 19%  ABS: 9% |
| >> ADOPTEE                       |
|                                  |
| Resolution 6/12 [suivante >]    |
+----------------------------------+
| [Rejoindre visio Zoom]          |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/meetings/{id}/resolutions",
         "POST /api/votes"]))

    story.append(screen(
        "3.8 Mon immeuble / Ma copropriete",
        "Infos copropriete, annuaire prestataires agrees, calendrier "
        "evenements, regles de vie.",
        """\
+----------------------------------+
| < Retour   Mon immeuble         |
+----------------------------------+
| Green City Bassam                |
| Syndic: ISIS Immobilier          |
| Tel: +225 27 XX XX XX           |
+----------------------------------+
| INFOS                            |
| 269 lots | 8 coproprietes        |
| Budget annuel: 108M FCFA        |
| Prochaine AG: 15/04/2026        |
|                                  |
| PRESTATAIRES AGREES              |
| ABC Maintenance — Plomberie     |
| SecuriGuard — Securite          |
| GreenPro — Espaces verts        |
| [Voir tous]                      |
|                                  |
| CALENDRIER                       |
| 15/04 — AG Ordinaire            |
| 22/04 — Debut travaux parking   |
| 01/05 — Coupure eau prevue      |
|                                  |
| REGLES DE VIE                    |
| [Consulter le reglement]         |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/co-ownerships/{id}",
         "GET /api/suppliers"]))

    story.append(screen(
        "3.9 CRG Bailleur",
        "Pour les proprietaires qui louent leur bien. Compte rendu de gestion : "
        "loyers percus, impayes locataire, rendement, documents locatifs.",
        """\
+----------------------------------+
| < Retour   CRG Bailleur        |
+----------------------------------+
| Mon bien loue: A-AC-101         |
| Locataire: M. Dje K.            |
| Loyer: 420 000 FCFA/mois        |
+----------------------------------+
| LOYERS PERCUS 2026               |
| Janvier:  420 000 [OK]          |
| Fevrier:  420 000 [OK]          |
| Mars:     420 000 [OK]          |
| Total:  1 260 000 FCFA          |
|                                  |
| RENDEMENT BRUT: 5.2%            |
| Gestion syndic: 6% = 25 200/m  |
| Net percu: 394 800 FCFA/mois    |
|                                  |
| DOCUMENTS                        |
| Bail signe 01/01/2024    [PDF]  |
| Etat lieux entree        [PDF]  |
| Attestation assurance    [PDF]  |
|                                  |
| ECHEANCES                        |
| Revision loyer: 01/01/2027      |
| Fin bail: 31/12/2026            |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/leases?ownerId={id}",
         "GET /api/payments?leaseId={id}"]))

    story.append(screen(
        "3.10 Mon profil",
        "Informations personnelles, parametres de notification, "
        "securite (changement mot de passe, 2FA optionnel).",
        """\
+----------------------------------+
| < Retour   Mon profil           |
+----------------------------------+
| [Avatar]                         |
| M. Jean-Marc Kouame             |
| jm@mail.ci                      |
| +225 07 XX XX XX                |
|                                  |
| Lot(s): V-012                   |
| Copro: Green City Horizontale   |
| Role: President CS              |
+----------------------------------+
| NOTIFICATIONS                    |
| [x] Email                       |
| [x] Push                        |
| [ ] SMS                         |
|                                  |
| SECURITE                         |
| [Changer mot de passe]          |
| [Activer 2FA]                    |
|                                  |
| [Modifier mes infos]            |
| [Se deconnecter]                 |
+----------------------------------+
|[Accueil][Compte][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/auth/me", "PUT /api/auth/me"]))

    story.append(PageBreak())

    # =========================================================
    # 4. MOBLOC
    # =========================================================
    story.append(Paragraph("4. MobLoc — PWA Locataire (:5054)", styles["AppTitle"]))
    story.append(Paragraph(
        "Application mobile simplifiee pour les ~300 locataires "
        "(residentiels et commerciaux). Paiement loyer mobile money, "
        "quittances PDF, incidents, documents, contact gestionnaire.",
        styles["SectionIntro"]))

    story.append(screen(
        "4.1 Accueil",
        "Prochain loyer avec montant et echeance, solde, bouton payer. "
        "Actions rapides (2x2). Notifications recentes.",
        """\
+----------------------------------+
| [Avatar] Bonjour Karim      [1] |
+----------------------------------+
|                                  |
| +------------------------------+ |
| | PROCHAIN LOYER               | |
| | 420 000 FCFA                 | |
| | Echeance: 05/04/2026        | |
| |                              | |
| | [Payer]      [Quittances]    | |
| +------------------------------+ |
|                                  |
| +--------+  +---------+         |
| |Signaler|  |  Mes    |         |
| |probleme|  | Docs    |         |
| +--------+  +---------+         |
| +--------+  +---------+         |
| |Contacter| | Infos  |         |
| |gestion. | |pratiques|         |
| +--------+  +---------+         |
|                                  |
| NOTIFICATIONS                    |
| Quittance mars dispo.  [Voir]   |
| Intervention 22/03     [Detail] |
|                                  |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/rent-calls?tenantId={id}",
         "GET /api/notifications"]))

    story.append(screen(
        "4.2 Mon loyer — Paiement",
        "Paiement par Orange Money, MTN, Wave. Option auto-prelevement. "
        "Historique des paiements.",
        """\
+----------------------------------+
| < Retour   Mon loyer            |
+----------------------------------+
| Lot: A-AC-101 | Bail actif      |
| Loyer: 420 000 FCFA/mois        |
| Charges: 59 000 FCFA/mois       |
| Total: 479 000 FCFA             |
+----------------------------------+
| PAYER                            |
| [x] Orange Money                |
| [ ] MTN Money                   |
| [ ] Wave                        |
| [ ] Virement                    |
|                                  |
| N. tel: [+225 01 __ __ __ __]   |
|                                  |
| [ ] Activer auto-prelevement    |
|     le 1er de chaque mois       |
|                                  |
| [  Payer 479 000 FCFA  ]        |
+----------------------------------+
| HISTORIQUE                       |
| Mars 2026: 479 000 [Paye]      |
| Fev 2026:  479 000 [Paye]      |
| Jan 2026:  479 000 [Paye]      |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["POST /api/cinetpay/initialize",
         "GET /api/payments?tenantId={id}"]))

    story.append(screen(
        "4.3 Mes quittances",
        "Telechargement des quittances de loyer en PDF. "
        "Historique mensuel.",
        """\
+----------------------------------+
| < Retour   Quittances           |
+----------------------------------+
| Lot: A-AC-101                   |
| Bailleur: Mme Bamba A.          |
+----------------------------------+
|                                  |
| Mars 2026                        |
| Loyer: 420 000 | Charges: 59 000|
| Total: 479 000 FCFA   [DL PDF] |
|                                  |
| Fevrier 2026                     |
| Loyer: 420 000 | Charges: 59 000|
| Total: 479 000 FCFA   [DL PDF] |
|                                  |
| Janvier 2026                     |
| Loyer: 420 000 | Charges: 59 000|
| Total: 479 000 FCFA   [DL PDF] |
|                                  |
| Decembre 2025                    |
| Loyer: 420 000 | Charges: 59 000|
| Total: 479 000 FCFA   [DL PDF] |
|                                  |
| [Voir tout l'historique]         |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/rent-receipts?tenantId={id}"]))

    story.append(screen(
        "4.4 Signaler un probleme",
        "Declaration d'incident avec photos, description et suivi en temps "
        "reel. Meme formulaire que MobProprio adapte locataire.",
        """\
+----------------------------------+
| < Retour   Signaler             |
+----------------------------------+
| +------------------------------+ |
| |   [  PRENDRE PHOTO  ]       | |
| +------------------------------+ |
|                                  |
| Mon logement: A-AC-101          |
|                                  |
| Type de probleme:                |
| [Plomberie_________________v]   |
|                                  |
| Description:                     |
| [Chasse d'eau ne fonctionne   ] |
| [plus depuis ce matin.        ] |
|                                  |
| [ Envoyer ]                      |
+----------------------------------+
| MES SIGNALEMENTS EN COURS        |
| +------------------------------+ |
| | I-48 Plomberie   [En cours] | |
| | Fuite robinet — 12/02       | |
| | Prestataire: ABC Maint.     | |
| | Intervention: 13/02 prevue  | |
| +------------------------------+ |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["POST /api/incidents",
         "GET /api/incidents?tenantId={id}"]))

    story.append(screen(
        "4.5 Mes documents",
        "Bail, etat des lieux, regles de vie, attestation assurance. "
        "Telechargement PDF.",
        """\
+----------------------------------+
| < Retour   Documents            |
+----------------------------------+
|                                  |
| MON BAIL                         |
| Bail signe 01/01/2024    [PDF]  |
| Duree: 3 ans (31/12/2026)       |
| Loyer: 420 000 FCFA/mois        |
|                                  |
| ETATS DES LIEUX                  |
| Entree 01/01/2024        [PDF]  |
|                                  |
| ASSURANCE                        |
| Attestation MRH 2026     [PDF]  |
|                                  |
| REGLES DE VIE                    |
| Reglement interieur      [PDF]  |
| Code bon voisinage       [PDF]  |
|                                  |
| REGULARISATION CHARGES           |
| Regularisation 2025      [PDF]  |
| Detail charges T4 2025   [PDF]  |
|                                  |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/documents?tenantId={id}"]))

    story.append(screen(
        "4.6 Contact gestionnaire",
        "Messagerie directe avec le gestionnaire locatif. "
        "Historique des echanges.",
        """\
+----------------------------------+
| < Retour   Contact              |
+----------------------------------+
| Gestionnaire: M. Diop           |
| ISIS Immobilier                  |
| Tel: +225 27 XX XX XX           |
+----------------------------------+
|                                  |
| [Syndic] Hier 16:30             |
| L'intervention plomberie est     |
| confirmee pour demain 22/03      |
| entre 8h et 10h.                |
|                                  |
| [Vous] Hier 14:00               |
| Bonjour, la chasse d'eau de     |
| la salle de bain ne fonctionne  |
| plus. Quand pouvez-vous         |
| intervenir ?                     |
|                                  |
| [Syndic] 15/03 10:00            |
| Bienvenue dans votre logement ! |
| N'hesitez pas a nous contacter  |
| pour toute question.            |
|                                  |
| [Message...________] [Envoyer]  |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/communication-messages?tenantId={id}",
         "POST /api/communication-messages"]))

    story.append(screen(
        "4.7 Infos pratiques",
        "Annuaire contacts utiles, calendrier evenements, "
        "regles de vie de la copropriete.",
        """\
+----------------------------------+
| < Retour   Infos pratiques      |
+----------------------------------+
| CONTACTS UTILES                  |
| Gardien: M. Brou — 07 XX XX XX |
| Urgences syndic: 27 XX XX XX   |
| Pompiers: 180                   |
| Police: 170                     |
|                                  |
| CALENDRIER                       |
| 22/03 — Intervention plomberie  |
| 01/04 — Coupure eau (8h-12h)   |
| 15/04 — AG (info)              |
|                                  |
| HORAIRES                         |
| Silence: 22h00 — 07h00          |
| Poubelles: mardi + vendredi     |
| Piscine: 7h-21h (residents)     |
|                                  |
| REGLES DE VIE                    |
| [Consulter le reglement]         |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/co-ownerships/{id}"]))

    story.append(screen(
        "4.8 Regularisation des charges",
        "Detail de la regularisation annuelle des charges locatives. "
        "Possibilite de contestation.",
        """\
+----------------------------------+
| < Retour   Regularisation       |
+----------------------------------+
| Exercice: 2025                   |
| Lot: A-AC-101                   |
+----------------------------------+
| DETAIL DES CHARGES               |
| Eau commune:        18 000 FCFA |
| Electricite commune: 12 000 FCFA|
| Entretien:          24 000 FCFA |
| Ascenseur:          36 000 FCFA |
| Securite:           48 000 FCFA |
| STEP:               24 000 FCFA |
|                                  |
| TOTAL CHARGES:     162 000 FCFA |
| Provisions versees: 177 000 FCFA|
|                                  |
| SOLDE: +15 000 FCFA (trop-percu)|
| >> Credit sur prochain loyer     |
|                                  |
| [Telecharger detail PDF]        |
| [Contester]                      |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/charge-regularizations?tenantId={id}"]))

    story.append(screen(
        "4.9 Mon profil",
        "Informations personnelles, parametres de notification, securite.",
        """\
+----------------------------------+
| < Retour   Mon profil           |
+----------------------------------+
| [Avatar]                         |
| M. Karim Dje                    |
| karim.dje@mail.ci               |
| +225 01 XX XX XX                |
|                                  |
| Logement: A-AC-101              |
| Immeuble: Acajou                |
| Bail: 01/01/24 — 31/12/26      |
+----------------------------------+
| NOTIFICATIONS                    |
| [x] Push (loyer, incidents)     |
| [x] Email (quittances)          |
| [ ] SMS                         |
|                                  |
| [Changer mot de passe]          |
| [Se deconnecter]                 |
+----------------------------------+
|[Accueil][Loyer][Signal][Doc][Msg]|
+----------------------------------+""",
        ["GET /api/auth/me"]))

    # Build
    doc.build(story)
    print(f"PDF genere: {OUTPUT}")


if __name__ == "__main__":
    build_pdf()
