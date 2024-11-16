import { existsSync, mkdirSync, writeFileSync } from "fs";
import { join } from "path";

const moduleName = process.argv[2];
if (!moduleName) {
  console.error(
    "❌ Please provide a module name: npm run create <module-name>"
  );
  process.exit(1);
}

const baseDir = join(process.cwd(), "src", "modules", moduleName);
const subDirs = ["context", "hook", "service", "type"];
const files = {
  context: `${moduleName}.context.tsx`,
  hook: `${moduleName}.hook.ts`,
  service: `${moduleName}.service.ts`,
  type: `${moduleName}.d.ts`,
};

const templates = {
  context: `import { createContext, ReactNode, useState } from "react";
import { I${capitalize(
    moduleName
  )}ContextProps } from "../type/${moduleName}.d";

export const ${capitalize(moduleName)}Context = createContext<I${capitalize(
    moduleName
  )}ContextProps | undefined>(undefined);

export const ${capitalize(
    moduleName
  )}Provider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<I${capitalize(
    moduleName
  )}ContextProps>({});

  return (
    <${capitalize(moduleName)}Context.Provider value={{ ...state, setState }}>
      {children}
    </${capitalize(moduleName)}Context.Provider>
  );
};
`,
  hook: `import { useContext } from "react";
import { ${capitalize(
    moduleName
  )}Context } from "../context/${moduleName}.context";

export const use${capitalize(moduleName)} = () => {
  const context = useContext(${capitalize(moduleName)}Context);
  if (!context) {
    throw new Error("use${capitalize(
      moduleName
    )} must be used within a ${capitalize(moduleName)}Provider");
  }
  return context;
};
`,
  service: `export const ${moduleName}Service = {
  // Add your service methods here
};
`,
  type: `export interface I${capitalize(moduleName)}ContextProps {
  // Define your context properties here
}
`,
};

function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

try {
  if (!existsSync(baseDir)) {
    mkdirSync(baseDir, { recursive: true });
    console.log(`✔️ Created module folder: ${baseDir}`);
  }

  subDirs.forEach((subDir) => {
    const subDirPath = join(baseDir, subDir);
    if (!existsSync(subDirPath)) {
      mkdirSync(subDirPath, { recursive: true });
      console.log(`✔️ Created subfolder: ${subDirPath}`);
    }

    const fileName = files[subDir];
    const filePath = join(subDirPath, fileName);
    if (!existsSync(filePath)) {
      writeFileSync(filePath, templates[subDir]);
      console.log(`✔️ Created file: ${filePath}`);
    }
  });

  console.log(`✅ Module "${moduleName}" created successfully.`);
} catch (error) {
  console.error("❌ Error creating module:", error);
}
