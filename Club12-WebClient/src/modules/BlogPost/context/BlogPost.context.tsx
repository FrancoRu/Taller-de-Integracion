import { createContext, ReactNode, useState } from "react";
import { IBlogPostContextProps } from "../type/BlogPost.d";

export const BlogPostContext = createContext<IBlogPostContextProps | undefined>(undefined);

export const BlogPostProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<IBlogPostContextProps>({});

  return (
    <BlogPostContext.Provider value={{ ...state, setState }}>
      {children}
    </BlogPostContext.Provider>
  );
};
