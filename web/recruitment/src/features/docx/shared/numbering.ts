// web/recruitment/src/features/docx/shared/numbering.ts
//
// Bullet lists in the .docx MUST come from a numbering config, never a literal
// "•" character (gotcha 2 in 6.6a — a literal bullet fails conformance C7).
// One config, referenced by every bulleted paragraph.

import { LevelFormat, AlignmentType, convertInchesToTwip } from "docx";

export const BULLET_NUMBERING = {
  config: [
    {
      reference: "cv-bullets",
      levels: [
        {
          level: 0,
          format: LevelFormat.BULLET,
          text: "\u2022", // the bullet GLYPH lives in the numbering definition, not in run text
          alignment: AlignmentType.LEFT,
          style: {
            paragraph: {
              indent: { left: convertInchesToTwip(0.25), hanging: convertInchesToTwip(0.15) },
            },
          },
        },
      ],
    },
  ],
};
