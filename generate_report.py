import os
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn

# ==============================================================================
# LOCKED PROJECT & ACADEMIC METADATA (CIPHERVAULT — ASP.NET CORE 10)
# ==============================================================================
PROJECT_TITLE    = "CipherVault: Personal Encrypted Password & Secure Notes Manager"
PROJECT_SUBTITLE = "A Full Stack Web Application Featuring AES Encryption at Rest, IDOR-Safe Access Control, and JWT REST API"
COURSE_NAME      = "Full Stack Web Development Project using ASP.NET Core"
COURSE_CODE      = "DSC-M-BCA-471P"
STUDENT_NAME     = "Kaswan Yogesh Subhashchandra"
ROLL_NO          = "32"
GUIDE_NAME       = "Prof. Sejal Shah"
SUBMISSION_DATE  = "16-10-2026"
UNIVERSITY_NAME  = "Gujarat University"
COLLEGE_NAME     = "Lokmanya College of Computer Applications"

# B&W Print-Optimized Monochrome Palette
BW_BLACK      = RGBColor(0, 0, 0)
BW_DARK_GRAY  = RGBColor(51, 51, 51)
BW_MUTED      = RGBColor(102, 102, 102)
HEX_BLACK     = "000000"
HEX_LIGHT_BG  = "FFFFFF"
HEX_ALT_ROW   = "F5F5F5"
HEX_BORDER    = "000000"

def get_image_path(filename):
    candidates = [
        filename,
        os.path.join("docs", "screenshots", filename),
        os.path.join("visualizations", filename),
        os.path.join("..", filename)
    ]
    for path in candidates:
        if os.path.exists(path):
            return path
    return None

def set_cell_background(cell, hex_color):
    shading = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>')
    cell._tc.get_or_add_tcPr().append(shading)

def set_cell_margins(cell, top=100, bottom=100, left=140, right=140):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def set_table_borders(table, color=HEX_BORDER):
    tblPr = table._tbl.tblPr
    borders = parse_xml(
        f'<w:tblBorders {nsdecls("w")}>'
        f'  <w:top w:val="single" w:sz="6" w:space="0" w:color="{color}"/>'
        f'  <w:bottom w:val="single" w:sz="8" w:space="0" w:color="{color}"/>'
        f'  <w:left w:val="none"/>'
        f'  <w:right w:val="none"/>'
        f'  <w:insideH w:val="single" w:sz="4" w:space="0" w:color="{color}"/>'
        f'  <w:insideV w:val="single" w:sz="4" w:space="0" w:color="{color}"/>'
        f'</w:tblBorders>'
    )
    tblPr.append(borders)

def add_page_number(run):
    fldChar1 = parse_xml(r'<w:fldChar %s w:fldCharType="begin"/>' % nsdecls('w'))
    instrText = parse_xml(r'<w:instrText %s xml:space="preserve"> PAGE </w:instrText>' % nsdecls('w'))
    fldChar2 = parse_xml(r'<w:fldChar %s w:fldCharType="separate"/>' % nsdecls('w'))
    fldChar3 = parse_xml(r'<w:fldChar %s w:fldCharType="end"/>' % nsdecls('w'))
    run._r.append(fldChar1)
    run._r.append(instrText)
    run._r.append(fldChar2)
    run._r.append(fldChar3)

def configure_document_styles(doc):
    for section in doc.sections:
        section.top_margin = Inches(0.95)
        section.bottom_margin = Inches(0.95)
        section.left_margin = Inches(1.0)
        section.right_margin = Inches(1.0)

        header = section.header
        hp = header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hrun = hp.add_run(f"{COURSE_NAME} ({COURSE_CODE})  |  CipherVault Project Report")
        hrun.font.name = "Calibri"
        hrun.font.size = Pt(8.5)
        hrun.font.color.rgb = BW_MUTED

        footer = section.footer
        fp = footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        
        frun_left = fp.add_run(f"Student: {STUDENT_NAME} (Roll: {ROLL_NO})  |  Guide: {GUIDE_NAME}")
        frun_left.font.name = "Calibri"
        frun_left.font.size = Pt(8.5)
        frun_left.font.color.rgb = BW_MUTED

        frun_right = fp.add_run("\t\tPage ")
        frun_right.font.name = "Calibri"
        frun_right.font.size = Pt(8.5)
        frun_right.font.color.rgb = BW_MUTED
        add_page_number(frun_right)

    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Calibri'
    normal_style.font.size = Pt(11)
    normal_style.font.color.rgb = BW_DARK_GRAY
    normal_style.paragraph_format.space_after = Pt(6)
    normal_style.paragraph_format.line_spacing = 1.15

def add_heading_1(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(16)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = "Calibri"
    run.font.size = Pt(17)
    run.font.bold = True
    run.font.color.rgb = BW_BLACK
    pBdr = parse_xml(
        f'<w:pBdr {nsdecls("w")}>'
        f'  <w:bottom w:val="single" w:sz="12" w:space="4" w:color="{HEX_BLACK}"/>'
        f'</w:pBdr>'
    )
    p._p.get_or_add_pPr().append(pBdr)
    return p

def add_heading_2(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = "Calibri"
    run.font.size = Pt(13.5)
    run.font.bold = True
    run.font.color.rgb = BW_BLACK
    return p

def add_body_p(doc, text, bold_prefix=None, space_after=6):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(space_after)
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    if bold_prefix:
        r_bold = p.add_run(bold_prefix)
        r_bold.font.name = "Calibri"
        r_bold.font.size = Pt(11)
        r_bold.font.bold = True
        r_bold.font.color.rgb = BW_BLACK
    r_text = p.add_run(text)
    r_text.font.name = "Calibri"
    r_text.font.size = Pt(11)
    r_text.font.color.rgb = BW_DARK_GRAY
    return p

def add_bullet_item(doc, bold_prefix, text):
    p = doc.add_paragraph(style='List Bullet')
    p.paragraph_format.space_after = Pt(3)
    if bold_prefix:
        r1 = p.add_run(bold_prefix)
        r1.font.name = "Calibri"
        r1.font.size = Pt(10.5)
        r1.font.bold = True
        r1.font.color.rgb = BW_BLACK
    r2 = p.add_run(text)
    r2.font.name = "Calibri"
    r2.font.size = Pt(10.5)
    r2.font.color.rgb = BW_DARK_GRAY
    return p

def add_callout_box(doc, title, lines):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    cell.width = Inches(6.5)
    set_cell_background(cell, HEX_LIGHT_BG)
    set_cell_margins(cell, top=120, bottom=120, left=180, right=160)
    tcPr = cell._tc.get_or_add_tcPr()
    borders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'  <w:top w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:bottom w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:left w:val="single" w:sz="24" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:right w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(borders)
    p0 = cell.paragraphs[0]
    p0.paragraph_format.space_after = Pt(4)
    r_title = p0.add_run(title)
    r_title.font.name = "Calibri"
    r_title.font.size = Pt(11)
    r_title.font.bold = True
    r_title.font.color.rgb = BW_BLACK
    for line in lines:
        p = cell.add_paragraph()
        p.paragraph_format.space_after = Pt(2)
        if isinstance(line, tuple):
            rb = p.add_run(line[0])
            rb.font.bold = True
            rb.font.color.rgb = BW_BLACK
            rt = p.add_run(line[1])
            rt.font.color.rgb = BW_DARK_GRAY
        else:
            p.add_run(line).font.color.rgb = BW_DARK_GRAY

def add_code_block(doc, cell_label, code_text, output_summary=None):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    cell.width = Inches(6.5)
    set_cell_background(cell, HEX_LIGHT_BG)
    set_cell_margins(cell, top=100, bottom=100, left=140, right=140)
    tcPr = cell._tc.get_or_add_tcPr()
    borders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'  <w:top w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:bottom w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:left w:val="single" w:sz="18" w:space="0" w:color="{HEX_BLACK}"/>'
        f'  <w:right w:val="single" w:sz="6" w:space="0" w:color="{HEX_BLACK}"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(borders)
    p_hdr = cell.paragraphs[0]
    p_hdr.paragraph_format.space_after = Pt(3)
    rh = p_hdr.add_run(f"Architecture Implementation — {cell_label}")
    rh.font.name = "Consolas"
    rh.font.size = Pt(9.5)
    rh.font.bold = True
    rh.font.color.rgb = BW_BLACK

    p_code = cell.add_paragraph()
    p_code.paragraph_format.space_after = Pt(2)
    rc = p_code.add_run(code_text.strip())
    rc.font.name = "Consolas"
    rc.font.size = Pt(9)
    rc.font.color.rgb = BW_BLACK

    if output_summary:
        p_out = cell.add_paragraph()
        p_out.paragraph_format.space_before = Pt(4)
        ro_lbl = p_out.add_run("Verified Behavior / Output:\n")
        ro_lbl.font.name = "Consolas"
        ro_lbl.font.size = Pt(9)
        ro_lbl.font.bold = True
        ro_lbl.font.color.rgb = BW_BLACK
        ro_txt = p_out.add_run(output_summary.strip())
        ro_txt.font.name = "Consolas"
        ro_txt.font.size = Pt(8.8)
        ro_txt.font.color.rgb = BW_DARK_GRAY

def add_styled_table(doc, headers, rows, col_widths=None):
    table = doc.add_table(rows=len(rows) + 1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(table)

    hdr_cells = table.rows[0].cells
    for i, h_text in enumerate(headers):
        hdr_cells[i].vertical_alignment = WD_ALIGN_VERTICAL.CENTER
        set_cell_background(hdr_cells[i], HEX_BLACK)
        set_cell_margins(hdr_cells[i], top=90, bottom=90, left=100, right=100)
        p = hdr_cells[i].paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT if i == 0 else WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(h_text)
        r.font.name = "Calibri"
        r.font.size = Pt(10)
        r.font.bold = True
        r.font.color.rgb = RGBColor(255, 255, 255)

    for r_idx, row_data in enumerate(rows):
        row_cells = table.rows[r_idx + 1].cells
        bg = HEX_ALT_ROW if r_idx % 2 == 1 else "FFFFFF"
        for c_idx, val in enumerate(row_data):
            row_cells[c_idx].vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            set_cell_background(row_cells[c_idx], bg)
            set_cell_margins(row_cells[c_idx], top=75, bottom=75, left=100, right=100)
            p = row_cells[c_idx].paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT if c_idx == 0 else WD_ALIGN_PARAGRAPH.CENTER
            r = p.add_run(str(val))
            r.font.name = "Calibri"
            r.font.size = Pt(9.8)
            r.font.bold = (c_idx == 0)
            r.font.color.rgb = BW_BLACK if c_idx == 0 else BW_DARK_GRAY

    if col_widths:
        for row in table.rows:
            for idx, w in enumerate(col_widths):
                row.cells[idx].width = Inches(w)
    return table

def add_plot_image(doc, filename, fig_num, caption_text, width_in=5.2):
    img_path = get_image_path(filename)
    p_img = doc.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(8)
    p_img.paragraph_format.space_after = Pt(3)
    p_img.paragraph_format.keep_with_next = True

    if img_path:
        p_img.add_run().add_picture(img_path, width=Inches(width_in))
    else:
        p_img.add_run(f"[Screenshot File: docs/screenshots/{filename}]").font.bold = True

    p_cap = doc.add_paragraph()
    p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_cap.paragraph_format.space_after = Pt(10)
    r_fig = p_cap.add_run(f"Figure {fig_num}: ")
    r_fig.font.bold = True
    r_cap = p_cap.add_run(caption_text)
    r_cap.font.italic = True
    r_cap.font.color.rgb = BW_MUTED


# ==============================================================================
# OFFICIAL SEQUENCE BUILDER (EXACT ACADEMIC REFERENCE MATCHED)
# ==============================================================================
def write_official_front_matter(doc):
    # 1. TITLE PAGE
    gu_logo = get_image_path("GU_logo.png")
    lok_logo = get_image_path("Lokmanya_Logo.png")

    if gu_logo or lok_logo:
        p_logo = doc.add_paragraph()
        p_logo.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_logo.paragraph_format.space_after = Pt(8)
        if lok_logo:
            p_logo.add_run().add_picture(lok_logo, width=Inches(1.1))
        if gu_logo:
            p_logo.add_run("        ")
            p_logo.add_run().add_picture(gu_logo, width=Inches(1.1))

    p_proj = doc.add_paragraph()
    p_proj.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_proj.paragraph_format.space_after = Pt(4)
    r_pr = p_proj.add_run("A PROJECT REPORT ON")
    r_pr.font.size = Pt(11)
    r_pr.font.bold = True

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_after = Pt(6)
    r_t = p_title.add_run(PROJECT_TITLE)
    r_t.font.size = Pt(15)
    r_t.font.bold = True

    p_sub = doc.add_paragraph()
    p_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_sub.paragraph_format.space_after = Pt(16)
    r_sub = p_sub.add_run(
        "Submitted in partial fulfilment of the requirements for the award of the degree of\n"
        "Bachelor of Computer Applications (Honours)\n"
        f"Course: {COURSE_NAME} (Course Code: {COURSE_CODE})"
    )
    r_sub.font.size = Pt(10)

    meta_table = doc.add_table(rows=1, cols=2)
    meta_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(meta_table, color="FFFFFF")

    cell_l = meta_table.cell(0, 0)
    cell_l.width = Inches(3.25)
    p_l = cell_l.paragraphs[0]
    p_l.add_run(f"PRESENTED BY\n{STUDENT_NAME} (Roll No: {ROLL_NO})\nBCA (Hons.)").font.bold = True

    cell_r = meta_table.cell(0, 1)
    cell_r.width = Inches(3.25)
    p_r = cell_r.paragraphs[0]
    p_r.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_r.add_run(f"SUBMITTED TO\n{UNIVERSITY_NAME}\nAhmedabad, Gujarat").font.bold = True

    p_guid = doc.add_paragraph()
    p_guid.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_guid.paragraph_format.space_before = Pt(18)
    p_guid.paragraph_format.space_after = Pt(12)
    r_g = p_guid.add_run(
        f"UNDER THE GUIDANCE OF\n{GUIDE_NAME}\nAssistant Professor\n{COLLEGE_NAME}\nAhmedabad, Gujarat, India\nAcademic Year 2026–27"
    )
    r_g.font.size = Pt(10)

    doc.add_page_break()

    # 2. COLLEGE CERTIFICATE
    if lok_logo:
        p_clogo = doc.add_paragraph()
        p_clogo.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_clogo.add_run().add_picture(lok_logo, width=Inches(1.2))

    p_cheader = doc.add_paragraph()
    p_cheader.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_cheader.paragraph_format.space_after = Pt(12)
    r_ch = p_cheader.add_run(f"{COLLEGE_NAME.upper()}\nAffiliated to {UNIVERSITY_NAME} · Ahmedabad, Gujarat\nCERTIFICATE")
    r_ch.font.size = Pt(13)
    r_ch.font.bold = True

    add_body_p(
        doc,
        f"This is to certify that the project report entitled “{PROJECT_TITLE}” is a bonafide record of "
        f"the work carried out by {STUDENT_NAME} and submitted to {UNIVERSITY_NAME} in partial fulfillment of the "
        f"requirements for the award of the degree of Bachelor of Computer Applications (Honors) for the course "
        f"{COURSE_NAME} ({COURSE_CODE}) during the academic year 2026–27."
    )
    add_body_p(
        doc,
        f"To the best of our knowledge, this work is original, was carried out at {COLLEGE_NAME}, Ahmedabad, "
        f"under the guidance of {GUIDE_NAME}, and has not been submitted in part or in full to any other university "
        f"or institution for the award of any degree or diploma."
    )

    p_sig_space = doc.add_paragraph()
    p_sig_space.paragraph_format.space_before = Pt(30)

    sig_table = doc.add_table(rows=1, cols=2)
    sig_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(sig_table, color="FFFFFF")

    cell_l = sig_table.cell(0, 0)
    cell_l.width = Inches(3.25)
    cell_l.paragraphs[0].add_run(f"_____________________________\n{GUIDE_NAME}\nProject Guide\n{COLLEGE_NAME}")

    cell_r = sig_table.cell(0, 1)
    cell_r.width = Inches(3.25)
    p_r = cell_r.paragraphs[0]
    p_r.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_r.add_run("_____________________________\nDr. Prakash Khuman\nDirector\nLokmanya College of Computer Applications")

    p_place = doc.add_paragraph()
    p_place.paragraph_format.space_before = Pt(18)
    p_place.add_run("Place: Ahmedabad\nDate: ____ / ____ / 2026").font.size = Pt(10)

    doc.add_page_break()

    # 3. INDEX / TABLE OF CONTENTS
    p_menu = doc.add_paragraph()
    p_menu.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_menu.paragraph_format.space_after = Pt(14)
    r_m = p_menu.add_run("INDEX / TABLE OF CONTENTS")
    r_m.font.size = Pt(16)
    r_m.font.bold = True

    toc_rows = [
        ["1.", "Title Page", "Page i"],
        ["2.", "College Certificate", "Page ii"],
        ["3.", "Index (Table of Contents)", "Page iii"],
        ["4.", "Acknowledgement", "Page iv"],
        ["5.", "Project Overview & Architecture Scope", "Page 5"],
        ["6.", "PHASE 1: MVC Setup, Base Layout & Authentication", "Page 6"],
        ["7.", "PHASE 2: Forms, Data Validation & Password Generator", "Page 7"],
        ["8.", "PHASE 3: Database Integration & Encrypted CRUD", "Page 9"],
        ["9.", "PHASE 4: REST API, JWT Authentication & Live Deployment", "Page 11"],
        ["10.", "Testing Verification Matrix & Challenges", "Page 13"],
        ["11.", "References & Bibliography", "Page 14"],
    ]
    add_styled_table(doc, ["Sr. No.", "Description", "Page No."], toc_rows, col_widths=[0.8, 4.5, 1.2])
    doc.add_page_break()

    # 4. ACKNOWLEDGEMENT
    p_ack = doc.add_paragraph()
    p_ack.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_ack.paragraph_format.space_after = Pt(16)
    r_a = p_ack.add_run("Acknowledgement")
    r_a.font.size = Pt(16)
    r_a.font.bold = True

    add_body_p(
        doc,
        "Completing this project has been a remarkable learning experience in enterprise web development and application security, "
        "and I am grateful to all those who guided and supported me along the way."
    )
    add_body_p(
        doc,
        f"First, I am deeply thankful to the management of {COLLEGE_NAME}, Ahmedabad, for providing state-of-the-art laboratory "
        "facilities, robust computing infrastructure, and a conducive academic environment."
    )
    add_body_p(
        doc,
        "I express my sincere gratitude to Dr. Prakash Khuman, Director, Lokmanya College of Computer Applications, "
        "for his constant encouragement and administrative support throughout the BCA (Honors) curriculum."
    )
    add_body_p(
        doc,
        f"I am profoundly indebted to my project guide, {GUIDE_NAME}, for her expert mentorship, invaluable technical insights, "
        "and rigorous code reviews during every stage of the CipherVault development lifecycle."
    )
    add_body_p(
        doc,
        f"I am also thankful to {UNIVERSITY_NAME} for incorporating the advanced course {COURSE_NAME} ({COURSE_CODE}) "
        "into our academic structure, bridging theoretical software engineering with secure production-grade implementation."
    )
    add_body_p(
        doc,
        "Finally, I extend my heartfelt appreciation to my family and peers for their constant motivation and patience."
    )

    p_auth = doc.add_paragraph()
    p_auth.paragraph_format.space_before = Pt(30)
    p_auth.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_auth.add_run(f"{STUDENT_NAME}\nBCA (Hons.) — Roll No: {ROLL_NO}\n{COLLEGE_NAME}").font.bold = True

    doc.add_page_break()


# ==============================================================================
# PROJECT CHAPTER BUILDERS
# ==============================================================================
def write_chapter_5_overview(doc):
    add_heading_1(doc, "5. Project Overview & Architecture Scope")
    add_callout_box(doc, "EXECUTIVE SUMMARY & SYSTEM SPECIFICATION", [
        ("Project Name: ", PROJECT_TITLE),
        ("Domain: ", "Personal Security / Encrypted Data Management / Full Stack Web Development"),
        ("Tech Stack: ", "ASP.NET Core 10 MVC, EF Core 10, SQL Server, ASP.NET Core Identity, Data Protection API (v10.0.7+)"),
        ("Key Security Features: ", "AES-authenticated encryption at rest, IDOR-safe 404 guards, dual auth (Cookie + JWT), HSTS enforced in production."),
        ("Live Deployment: ", "Hosted on MonsterASP via HTTPS (https://ciphervault.tryasp.net).")
    ])
    add_body_p(
        doc,
        "Managing personal credentials securely in a digital world fraught with breaches and credential stuffing is a critical challenge. "
        "Standard note-taking applications and spreadsheets store passwords in plaintext, exposing users to severe security risks. "
        "CipherVault is engineered as a robust, professional password and secure notes manager that demonstrates enterprise-grade "
        "cryptographic separation, ownership scoping, and modern web application architecture."
    )
    add_heading_2(doc, "5.1 Core Architectural Principles")
    add_bullet_item(doc, "Strict Layering: ", "Business logic is strictly isolated in Service classes. Controllers remain thin (validate request → call service → return view/response). DbContext is never injected into controllers.")
    add_bullet_item(doc, "Repository Pattern: ", "Mandatory for VaultItem entities to encapsulate encryption and ownership verification, ensuring clean data access boundaries.")
    add_bullet_item(doc, "DTO Separation: ", "Create and Update DTOs are strictly separated. The update pipeline implements a robust 'blank-preserve' rule to prevent accidental overwrites of encrypted secrets.")
    add_bullet_item(doc, "IDOR Protection: ", "Unauthorized or non-owned item access attempts consistently return HTTP 404 Not Found rather than 403 Forbidden, preventing resource enumeration attacks.")

    doc.add_page_break()

def write_chapter_6_phase1(doc):
    add_heading_1(doc, "6. PHASE 1: MVC Setup, Base Layout & Authentication")
    add_callout_box(doc, "PHASE 1 ALIGNMENT — ARCHITECTURE & SECURITY BASELINE", [
        ("Objective: ", "Configure ASP.NET Core 10 architecture and establish secure cookie authentication."),
        ("Key Deliverables: ", "MVC project skeleton targeting net10.0, ASP.NET Core Identity configuration, responsive Bootstrap 5 layout, and authorized dashboard placeholder.")
    ])
    add_body_p(
        doc,
        "Phase 1 establishes the foundation of CipherVault. The project was initialized targeting .NET 10 (net10.0) with "
        "Entity Framework Core 10 and ASP.NET Core Identity. To ensure local development parity without compromising security, "
        "cookie authentication was configured with unconditional SecurePolicy = Always and a 2-hour sliding expiration."
    )
    add_code_block(
        doc,
        "Program.cs — Authentication & Cookie Security Configuration",
        "builder.Services.AddDefaultIdentity<IdentityUser>(options => {\n"
        "    options.SignIn.RequireConfirmedAccount = false;\n"
        "    options.Password.RequiredLength = 8;\n"
        "    options.Lockout.MaxFailedAccessAttempts = 5;\n"
        "    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);\n"
        "}).AddEntityFrameworkStores<ApplicationDbContext>();\n\n"
        "builder.Services.ConfigureApplicationCookie(options => {\n"
        "    options.Cookie.Name = \"CipherVault.Auth\";\n"
        "    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;\n"
        "    options.Cookie.HttpOnly = true;\n"
        "    options.Cookie.SameSite = SameSiteMode.Lax;\n"
        "    options.ExpireTimeSpan = TimeSpan.FromHours(2);\n"
        "});",
        "Configured Identity with 5-attempt lockout and unconditional SecurePolicy=Always."
    )
    add_body_p(
        doc,
        "The base layout (_Layout.cshtml) was built using Bootstrap 5 with a responsive navigation bar displaying the logged-in user "
        "and logout actions. HomeController handles static landing pages, while DashboardController is protected with [Authorize]."
    )
    doc.add_page_break()

def write_chapter_7_phase2(doc):
    add_heading_1(doc, "7. PHASE 2: Forms, Data Validation & Password Generator")
    add_callout_box(doc, "PHASE 2 ALIGNMENT — USER INTERACTION & VALIDATION", [
        ("Objective: ", "Implement secure interaction forms, DTO separation, and client-side cryptographic utilities."),
        ("Key Deliverables: ", "VaultItem model, separate Create/Update DTOs, data annotations, custom validation attributes, and Web Crypto password generator.")
    ])
    add_body_p(
        doc,
        "Phase 2 focuses on user interaction and data integrity. The VaultItem model was designed with an integer ID, user foreign key, "
        "title, category enum (Password, Note, API Key, Credit Card, Other), optional username and URL, and encrypted secret and notes fields."
    )
    add_heading_2(doc, "7.1 Client-Side Crypto-Secure Password Generator")
    add_body_p(
        doc,
        "To prevent weak user passwords, a built-in password generator was developed in JavaScript using the Web Crypto API "
        "(window.crypto.getRandomValues()), ensuring cryptographically secure random byte selection rather than pseudo-random Math.random()."
    )
    add_code_block(
        doc,
        "wwwroot/js/password-generator.js — Secure Random Generation",
        "function generatePassword(length = 16) {\n"
        "    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+';\n"
        "    const array = new Uint32Array(length);\n"
        "    window.crypto.getRandomValues(array);\n"
        "    return Array.from(array, x => chars[x % chars.length]).join('');\n"
        "}",
        "Generated cryptographically secure random passwords avoiding Math.random()."
    )
    add_plot_image(doc, "vault/09-create-form.png", "7.1", "Vault Item creation form featuring the crypto-secure password generator and live strength meter.")
    doc.add_page_break()

def write_chapter_8_phase3(doc):
    add_heading_1(doc, "8. PHASE 3: Database Integration & Encrypted CRUD")
    add_callout_box(doc, "PHASE 3 ALIGNMENT — PERSISTENCE & ENCRYPTION AT REST", [
        ("Objective: ", "Establish EF Core persistence, repository pattern, and AES encryption at rest via Data Protection API."),
        ("Key Deliverables: ", "ApplicationDbContext, IVaultItemRepository, VaultItemService with AES encryption, IDOR-safe 404 guards, and search/filter.")
    ])
    add_body_p(
        doc,
        "Phase 3 is the core security milestone of CipherVault. Sensitive fields (Secret and Notes) are encrypted at rest using "
        "the ASP.NET Core Data Protection API. Two immutable purpose strings serve as a cryptographic compatibility contract:"
    )
    add_bullet_item(doc, "Secret Purpose: ", "\"CipherVault.Secret\" protecting the EncryptedSecret column.")
    add_bullet_item(doc, "Notes Purpose: ", "\"CipherVault.Notes\" protecting the EncryptedNotes column.")
    add_body_p(
        doc,
        "Metadata (Title, Username, URL, Category) remains unencrypted in the database to enable efficient server-side LINQ filtering "
        "and pagination without incurring full-table decryption overhead."
    )
    add_plot_image(doc, "security/15-db-encrypted-ssms.png", "8.1", "SSMS database inspection confirming that plaintext secrets never touch the database columns.")
    add_plot_image(doc, "vault/12-details-revealed.png", "8.2", "Vault Item details view showing masked defaults and authorized on-demand decryption.")
    doc.add_page_break()

def write_chapter_9_phase4(doc):
    add_heading_1(doc, "9. PHASE 4: REST API, JWT Authentication & Live Deployment")
    add_callout_box(doc, "PHASE 4 ALIGNMENT — API & PRODUCTION DEPLOYMENT", [
        ("Objective: ", "Expose a secure REST API with JWT Bearer auth, global error middleware, and live HTTPS deployment."),
        ("Key Deliverables: ", "VaultApiController, AuthApiController, dual auth scheme configuration, Postman collection, and MonsterASP deployment.")
    ])
    add_body_p(
        doc,
        "Phase 4 delivers enterprise interoperability and production hardening. A REST API was developed under /api/v1/vault "
        "reusing the core IVaultItemService. Dual authentication scheme separation was implemented in Program.cs: MVC controllers "
        "use Identity cookies, while API controllers require explicit JWT Bearer authorization."
    )
    add_plot_image(doc, "api/20-postman-login.png", "9.1", "Postman verification: POST /api/auth/login issuing a valid JWT Bearer token.")
    add_plot_image(doc, "api/22-postman-401.png", "9.2", "Postman verification: Unauthenticated API request returning HTTP 401 JSON without HTML redirect.")
    add_plot_image(doc, "api/23-postman-404-idor.png", "9.3", "Postman verification: Cross-user IDOR access attempt returning HTTP 404 Not Found.")
    doc.add_page_break()

def write_chapter_10_testing(doc):
    add_heading_1(doc, "10. Testing Verification Matrix & Challenges")
    add_callout_box(doc, "SYSTEM VERIFICATION & RETROSPECTIVE", [
        ("Verification: ", "11 critical test cases verified across security invariants, CRUD workflows, API responses, and deployment stability."),
        ("Deployment URL: ", "https://ciphervault.tryasp.net (MonsterASP Windows Hosting with Let's Encrypt SSL).")
    ])
    test_rows = [
        ["IDOR Cross-User Access", "Attempting to access another user's vault item returns 404 Not Found", "PASS ✔"],
        ["Blank Secret on Update", "Submitting an edit form with a blank secret preserves existing ciphertext", "PASS ✔"],
        ["Blank Notes on Update", "Leaving notes blank preserves them; empty string clears them to null", "PASS ✔"],
        ["API Missing Token", "Calling /api/v1/vault without a token returns HTTP 401 JSON (no redirect)", "PASS ✔"],
        ["API ?decrypt=true", "Supplying ?decrypt=true query parameter is rejected with HTTP 400", "PASS ✔"],
        ["Account Lockout", "5 failed login attempts trigger a 15-minute account lockout (HTTP 423)", "PASS ✔"],
        ["HSTS Header", "Production environment emits Strict-Transport-Security header", "PASS ✔"],
        ["Encryption at Rest", "Database inspection confirms only ciphertext is stored in Secret/Notes columns", "PASS ✔"],
        ["Key Persistence", "Application restart does not invalidate existing ciphertext (Key ring persists)", "PASS ✔"],
        ["Global Exception Handling", "Unhandled exceptions return JSON 500 for API and custom error page for MVC", "PASS ✔"],
        ["Mobile Responsiveness", "Bootstrap 5 layout adapts cleanly across mobile and desktop viewports", "PASS ✔"]
    ]
    add_styled_table(doc, ["Test Case Description", "Expected Behavior / Requirement", "Status"], test_rows, col_widths=[2.4, 3.1, 1.0])
    doc.add_page_break()

def write_chapter_11_references(doc):
    add_heading_1(doc, "11. References & Bibliography")
    add_bullet_item(doc, "[1] Microsoft Docs: ", "ASP.NET Core 10 Overview, Entity Framework Core 10, and ASP.NET Core Identity documentation. https://learn.microsoft.com/")
    add_bullet_item(doc, "[2] ASP.NET Core Security: ", "Data Protection API Architecture and Key Management. https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/")
    add_bullet_item(doc, "[3] OWASP Foundation: ", "Top Ten Web Application Security Risks — A01: Broken Access Control (IDOR) & Authentication Best Practices. https://owasp.org/")
    add_bullet_item(doc, "[4] Web Cryptography API: ", "W3C Recommendation for crypto.getRandomValues(). https://www.w3.org/TR/WebCryptoAPI/")
    add_bullet_item(doc, "[5] Course Curriculum: ", f"{COURSE_NAME} ({COURSE_CODE}), guided by {GUIDE_NAME} at {COLLEGE_NAME} ({UNIVERSITY_NAME}), Academic Year 2026–27.")


# ==============================================================================
# GENERATE MASTER PROJECT REPORT DOCX
# ==============================================================================
def generate_project_report():
    report_doc = Document()
    configure_document_styles(report_doc)

    write_official_front_matter(report_doc)
    write_chapter_5_overview(report_doc)
    report_doc.add_page_break()
    write_chapter_6_phase1(report_doc)
    report_doc.add_page_break()
    write_chapter_7_phase2(report_doc)
    report_doc.add_page_break()
    write_chapter_8_phase3(report_doc)
    report_doc.add_page_break()
    write_chapter_9_phase4(report_doc)
    report_doc.add_page_break()
    write_chapter_10_testing(report_doc)
    report_doc.add_page_break()
    write_chapter_11_references(report_doc)

    output_filename = "CipherVault_Project_Report.docx"
    report_doc.save(output_filename)

    print("=" * 75)
    print(f"✅ PROFESSIONAL B&W PROJECT REPORT GENERATED: {os.path.abspath(output_filename)}")
    print(f"   • Student: {STUDENT_NAME} (Roll No: {ROLL_NO}) | Guide: {GUIDE_NAME}")
    print(f"   • Course: {COURSE_NAME} ({COURSE_CODE})")
    print(f"   • Institution: {COLLEGE_NAME} ({UNIVERSITY_NAME})")
    print("=" * 75)

if __name__ == "__main__":
    generate_project_report()